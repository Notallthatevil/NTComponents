using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers.Tests;

public sealed class NTDataGridRules_Tests {
    private const string Items = "builder.AddComponentParameter(1, \"Items\", Array.Empty<string>().AsQueryable());";
    private const string ItemsProvider = "builder.AddComponentParameter(2, \"ItemsProvider\", new object());";

    private static Task<ImmutableArray<Diagnostic>> AnalyzeGridAsync(params string[] parameters) => AnalyzerHarness.AnalyzeBuildAsync($$"""
        builder.OpenComponent<NTDataGrid<string>>(0);
        {{string.Join(Environment.NewLine, parameters)}}
        builder.CloseComponent();
        """);

    [Fact]
    public async Task Reports_Both_Sources() {
        var diagnostics = await AnalyzeGridAsync(Items, ItemsProvider);

        Assert.Equal(["NTC1065"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(1, \"Items\", null);")]
    public async Task Reports_Missing_Source(string items) {
        var diagnostics = await AnalyzeGridAsync(items, "builder.AddComponentParameter(2, \"ItemsProvider\", (object?)null);");

        Assert.Equal(["NTC1066"], diagnostics.Ids());
    }

    [Fact]
    public async Task A_Null_Source_Does_Not_Count_As_Supplied() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(2, \"ItemsProvider\", (object?)null);");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_A_Missing_Source_When_Attributes_Are_Splatted() {
        var diagnostics = await AnalyzeGridAsync("builder.AddMultipleAttributes(1, new System.Collections.Generic.Dictionary<string, object>());");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Virtualized_Pagination() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"Virtualize\", true);", "builder.AddComponentParameter(4, \"ShowPagination\", true);");

        Assert.Equal(["NTC1067"], diagnostics.Ids());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DoesNotReport_Virtualization_Or_Pagination_Alone(bool virtualize, bool pagination) {
        var diagnostics = await AnalyzeGridAsync(Items, $"builder.AddComponentParameter(3, \"Virtualize\", {virtualize.ToString().ToLowerInvariant()});", $"builder.AddComponentParameter(4, \"ShowPagination\", {pagination.ToString().ToLowerInvariant()});");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Values_Known_Only_At_Runtime() {
        var diagnostics = await AnalyzerHarness.AnalyzeBuildAsync("""
            var enabled = DateTime.Now.Year > 2000;
            var size = DateTime.Now.Year - 3000;
            builder.OpenComponent<NTDataGrid<string>>(0);
            builder.AddComponentParameter(1, "Items", Array.Empty<string>().AsQueryable());
            builder.AddComponentParameter(2, "Virtualize", enabled);
            builder.AddComponentParameter(3, "ShowPagination", enabled);
            builder.AddComponentParameter(4, "PersistPrerenderedItems", enabled);
            builder.AddComponentParameter(5, "PageSize", size);
            builder.AddComponentParameter(6, "VirtualizationItemSize", (float)size);
            builder.CloseComponent();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_A_Persistence_Key_Without_PersistPrerenderedItems() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"PersistenceKey\", \"orders\");");

        Assert.Equal(["NTC1075"], diagnostics.Ids());
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.AddComponentParameter(4, \"PersistenceKey\", null);")]
    [InlineData("builder.AddComponentParameter(4, \"PersistenceKey\", \" \");")]
    public async Task Reports_Persistence_Without_A_Key(string key) {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"PersistPrerenderedItems\", true);", key);

        Assert.Equal(["NTC1076"], diagnostics.Ids());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DoesNotReport_Complete_Persistence_Configuration(bool enabled) {
        var diagnostics = await AnalyzeGridAsync(Items, $"builder.AddComponentParameter(3, \"PersistPrerenderedItems\", {enabled.ToString().ToLowerInvariant()});", "builder.AddComponentParameter(4, \"PersistenceKey\", \"orders\");");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Disabled_Persistence_Without_A_Key() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"PersistPrerenderedItems\", false);");

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-5", "-5")]
    public async Task Reports_A_Non_Positive_PageSize_As_Adjusted(string value, string shown) {
        var diagnostics = await AnalyzeGridAsync(Items, $"builder.AddComponentParameter(3, \"PageSize\", {value});");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1103", diagnostic.Id);
        Assert.Equal($"NTDataGrid PageSize value {shown} is changed to 10 at runtime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Reports_A_Virtualized_Row_Height_Below_One_As_Adjusted() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"Virtualize\", true);", "builder.AddComponentParameter(4, \"VirtualizationItemSize\", 0.5f);");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1103", diagnostic.Id);
        Assert.Equal("NTDataGrid VirtualizationItemSize value 0.5 is changed to 1 at runtime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DoesNotReport_Valid_Sizes() {
        var diagnostics = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"PageSize\", 25);", "builder.AddComponentParameter(4, \"Virtualize\", true);", "builder.AddComponentParameter(5, \"VirtualizationItemSize\", 1f);");
        var notVirtualized = await AnalyzeGridAsync(Items, "builder.AddComponentParameter(3, \"VirtualizationItemSize\", 0.5f);");

        Assert.Empty(diagnostics);
        Assert.Empty(notVirtualized);
    }

    [Theory]
    [InlineData("public string FullName => FirstName + LastName;")]
    [InlineData("public string FullName { get { return FirstName + LastName; } }")]
    [InlineData("public string FullName => _firstName + _lastName;")]
    [InlineData("public string FullName => this.FirstName + this.LastName;")]
    public async Task Reports_A_Computed_Aggregate_Property(string fullName) {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(PropertyColumnSource(fullName, "row => row.FullName"));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("NTC1068", diagnostic.Id);
        Assert.Contains("'FullName'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public string FullName { get; set; } = \"\";", "row => row.FullName")]
    [InlineData("public string Other => FirstName;", "row => row.Other")]
    [InlineData("public string FullName => Parts.First + Parts.Last;", "row => row.FullName")]
    [InlineData("public string FullName { get { var FirstName = \"A\"; var LastName = \"B\"; return FirstName + LastName; } }", "row => row.FullName")]
    [InlineData("public string FullName => FirstName + FirstName;", "row => row.FullName")]
    [InlineData("public string FullName => Static1 + Static2;", "row => row.FullName")]
    [InlineData("public string FullName => FirstName + LastName;", "row => row.FirstName + row.LastName")]
    public async Task DoesNotReport_Properties_The_Heuristic_Cannot_Prove_Are_Computed(string fullName, string lambda) {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(PropertyColumnSource(fullName, lambda));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_A_Selector_That_Is_Not_A_Plain_Property() {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(PropertyColumnSource(
            "public string FullName => FirstName + LastName;",
            "row => row.FirstName.Length > 0 ? row.FullName : row.LastName"));

        Assert.Empty(diagnostics);
    }

    private static string PropertyColumnSource(string fullName, string lambda) => $$"""
        using System;
        using System.Linq.Expressions;
        using Microsoft.AspNetCore.Components.CompilerServices;
        using Microsoft.AspNetCore.Components.Rendering;
        using NTComponents;

        public sealed class NameParts {
            public string First { get; set; } = "";
            public string Last { get; set; } = "";
        }

        public sealed class Row {
            private readonly string _firstName = "";
            private readonly string _lastName = "";
            public static string Static1 { get; set; } = "";
            public static string Static2 { get; set; } = "";
            public NameParts Parts { get; } = new();
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            {{fullName}}
        }

        public static class Fixture {
            public static void Build(RenderTreeBuilder builder) {
                builder.OpenComponent<NTPropertyColumn<Row, string>>(0);
                builder.AddComponentParameter(1, "Property", RuntimeHelpers.TypeCheck<Expression<Func<Row, string>>>({{lambda}}));
                builder.CloseComponent();
            }
        }
        """;
}
