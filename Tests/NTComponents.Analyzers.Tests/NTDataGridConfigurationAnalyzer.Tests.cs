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

    [Fact]
    public async Task Reports_Exact_Missing_Source_Diagnostics_From_NonGeneric_And_Nested_Executable_Shapes() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class GridFactory {
    public GridFactory(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTDataGrid<Row>));
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local() {
            builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(1);
            builder.CloseComponent();
        }

        Action<RenderTreeBuilder> parenthesized = (nestedBuilder) => {
            nestedBuilder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(2);
            nestedBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> simple = nestedBuilder => {
            nestedBuilder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(3);
            nestedBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> anonymous = delegate(RenderTreeBuilder nestedBuilder) {
            nestedBuilder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(4);
            nestedBuilder.CloseComponent();
        };
    }
}

public sealed class Row { }
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("GridFactory.razor.g.cs", source));

        Assert.Equal(5, diagnostics.Length);
        Assert.All(diagnostics, static diagnostic => {
            Assert.Equal(NTDataGridConfigurationAnalyzer.MissingSourceDiagnosticId, diagnostic.Id);
            Assert.Equal("NTDataGrid requires Items or ItemsProvider", diagnostic.GetMessage());
        });
    }

    [Fact]
    public async Task Reports_Null_Sources_And_Razor_TypeChecked_Virtualized_Pagination() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class GridFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(0);
        builder.AddComponentParameter(1, "Items", RuntimeHelpers.TypeCheck<object?>(null));
        builder.AddComponentParameter(2, "ItemsProvider", RuntimeHelpers.TypeCheck<object?>(null));
        builder.AddComponentParameter(3, "Virtualize", RuntimeHelpers.TypeCheck<bool>(true));
        builder.AddComponentParameter(4, "ShowPagination", RuntimeHelpers.TypeCheck<bool>(true));
        builder.CloseComponent();
    }
}

public sealed class Row { }

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("GridFactory.razor.g.cs", source));

        Assert.Equal(
            [
                NTDataGridConfigurationAnalyzer.MissingSourceDiagnosticId,
                NTDataGridConfigurationAnalyzer.VirtualizedPaginationDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
        Assert.Equal("RuntimeHelpers.TypeCheck<bool>(true)", diagnostics[1].Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostics[1].Location.SourceSpan));
    }

    [Fact]
    public async Task DoesNotReport_Dynamic_Attributes_Other_Components_Or_Unclosed_Grid() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class GridFactory {
    public static void Build(RenderTreeBuilder builder, object items, bool enabled, string attributeName) {
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(0);
        builder.AddAttribute(1, "Items", items);
        builder.AddAttribute(2, "Virtualize", enabled);
        builder.AddAttribute(3, "ShowPagination", enabled);
        builder.AddAttribute(4, attributeName, new object());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.OtherComponent>(5);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTDataGrid<Row>>(6);
        builder.AddAttribute(7, "Items", items);
    }
}

public sealed class Row { }
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("GridFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("public string FullName => _firstName + _lastName;", true)]
    [InlineData("public string FullName => base.FirstName + LastName;", true)]
    [InlineData("public string FullName => Other.FirstName + Other.LastName;", false)]
    [InlineData("public string FullName { get { var FirstName = \"A\"; var LastName = \"B\"; return FirstName + LastName; } }", false)]
    [InlineData("public string FullName => FirstName + FirstName;", false)]
    [InlineData("public string FullName => StaticFirstName + StaticLastName;", false)]
    public async Task Distinguishes_Computed_Aggregates_From_Translatable_Or_Local_Properties(string fullNameProperty, bool shouldReport) {
        var source = GetPropertyColumnSource(
            $$"""
            private readonly string _firstName = "";
            private readonly string _lastName = "";
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public static string StaticFirstName { get; set; } = "";
            public static string StaticLastName { get; set; } = "";
            public NameParts Other { get; } = new();
            {{fullNameProperty}}
            """,
            "row => row.FullName",
            "public sealed class NameParts { public string FirstName { get; set; } = \"\"; public string LastName { get; set; } = \"\"; }",
            fullNameProperty.Contains("base.", StringComparison.Ordinal) ? "public string FirstName { get; set; } = \"\";" : string.Empty);

        var diagnostics = await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source));

        if (shouldReport) {
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(NTDataGridConfigurationAnalyzer.ComputedPropertySortDiagnosticId, diagnostic.Id);
            Assert.Contains("FullName", diagnostic.GetMessage(), StringComparison.Ordinal);
        }
        else {
            Assert.Empty(diagnostics);
        }
    }

    [Fact]
    public async Task DoesNotReport_Property_Delegate_With_Multiple_Return_Paths() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public static class PropertyColumnFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTPropertyColumn<Row, string>>(0);
        builder.AddAttribute(1, "Property", (Func<Row, string>)(row => {
            if (row.FirstName.Length > 0) {
                return row.FirstName;
            }
            return row.LastName;
        }));
        builder.CloseComponent();
    }
}

public sealed class Row {
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("PropertyColumnFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    private static string GetPropertyColumnSource(string rowMembers, string propertyExpression, string additionalTypes = "", string rowBaseMembers = "") => $$"""
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

public sealed class Row : RowBase {
{{rowMembers}}
}

{{additionalTypes}}

public abstract class RowBase { {{rowBaseMembers}} }

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

        Assert.DoesNotContain(compilation.GetDiagnostics(TestContext.Current.CancellationToken), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var analyzer = new NTDataGridConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void OpenComponent(int sequence, global::System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTDataGrid<TItem> where TItem : class { }
    public class NTPropertyColumn<TItem, TValue> where TItem : class { }
    public class OtherComponent { }
}
""";
}
