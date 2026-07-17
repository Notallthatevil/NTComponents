using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTDataGridConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Duplicate_Source_And_Virtualized_Pagination() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class GridFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(0);
        builder.AddAttribute(1, "Items", new object());
        builder.AddAttribute(2, "ItemsProvider", new object());
        builder.AddAttribute(3, "Virtualize", true);
        builder.AddAttribute(4, "ShowPagination", true);
        builder.CloseComponent();
    }
}

public sealed class Row { }

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTDataGrid<TItem> where TItem : class { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("GridFactory.cs", source));

        Assert.Equal(
            [
                NTDataGridConfigurationAnalyzer.DuplicateSourceDiagnosticId,
                NTDataGridConfigurationAnalyzer.VirtualizedPaginationDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Missing_Source() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class GridFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(0);
        builder.CloseComponent();
    }
}

public sealed class Row { }

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTDataGrid<TItem> where TItem : class { }
}
""";

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("GridFactory.cs", source)));

        Assert.Equal(NTDataGridConfigurationAnalyzer.MissingSourceDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReport_For_One_Source_And_NonVirtualizedPagination() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class GridFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(0);
        builder.AddAttribute(1, "Items", new object());
        builder.AddAttribute(2, "Virtualize", false);
        builder.AddAttribute(3, "ShowPagination", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(4);
        builder.AddAttribute(5, "ItemsProvider", new object());
        builder.AddAttribute(6, "Virtualize", true);
        builder.AddAttribute(7, "ShowPagination", false);
        builder.CloseComponent();
    }
}

public sealed class Row { }

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTDataGrid<TItem> where TItem : class { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("GridFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("public string FullName => FirstName + LastName;")]
    [InlineData("public string FullName { get { return FirstName + LastName; } }")]
    public async Task Reports_Computed_Aggregate_Property_Column(string fullNameProperty) {
        var source = GetPropertyColumnSource(
            $$"""
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            {{fullNameProperty}}
            """,
            "row => row.FullName");

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source)));

        Assert.Equal(NTDataGridConfigurationAnalyzer.ComputedPropertySortDiagnosticId, diagnostic.Id);
        Assert.Contains("FullName", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReport_Auto_Property_Column() {
        var source = GetPropertyColumnSource(
            "public string FullName { get; set; } = \"\";",
            "row => row.FullName");

        var diagnostics = await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Translatable_Inline_Expression() {
        var source = GetPropertyColumnSource(
            """
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            """,
            "row => row.FirstName + row.LastName");

        var diagnostics = await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_Computed_Property_With_One_Dependency() {
        var source = GetPropertyColumnSource(
            """
            public string FirstName { get; set; } = "";
            public string DisplayName => FirstName;
            """,
            "row => row.DisplayName");

        var diagnostics = await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    private static string GetPropertyColumnSource(string rowMembers, string propertyExpression) => $$"""
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;

public static class PropertyColumnFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTPropertyColumn<Row, string>>(0);
        builder.AddComponentParameter(1, "Property", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<Expression<Func<Row, string>>>({{propertyExpression}}));
        builder.CloseComponent();
    }
}

public sealed class Row {
{{rowMembers}}
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public class NTDataGrid<TItem> where TItem : class { }
    public class NTPropertyColumn<TItem, TValue> where TItem : class { }
}
""";

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(params (string Path, string Source)[] sources) {
        var syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(
                source.Source,
                new CSharpParseOptions(LanguageVersion.Latest),
                source.Path))
            .ToImmutableArray();

        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new NTDataGridConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }
}
