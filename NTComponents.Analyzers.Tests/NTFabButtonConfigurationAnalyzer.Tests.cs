using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTFabButtonConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Missing_Required_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabButton>(0);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabButton>(1);
        builder.AddAttribute(2, "Icon", null);
        builder.AddAttribute(3, "Label", " ");
        builder.AddAttribute(4, "AriaLabel", " ");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabButtonFactory.cs", source));

        Assert.Equal(
            [
                NTFabButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTFabButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTFabButtonConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId,
                NTFabButtonConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Invalid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabButton>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(2, "Label", "Create\nitem");
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(5, "ButtonSize", global::NTComponents.Size.Smallest);
        builder.AddAttribute(6, "Placement", (global::NTComponents.NTFabButtonPlacement)999);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabButton>(7);
        builder.AddAttribute(8, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(9, "AriaLabel", "Create item");
        builder.AddAttribute(10, "ButtonSize", global::NTComponents.Size.XL);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabButtonFactory.cs", source));

        Assert.Equal(
            [
                NTFabButtonConfigurationAnalyzer.InvalidLabelDiagnosticId,
                NTFabButtonConfigurationAnalyzer.InvisibleBackgroundDiagnosticId,
                NTFabButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTFabButtonConfigurationAnalyzer.UnsupportedSizeDiagnosticId,
                NTFabButtonConfigurationAnalyzer.UnsupportedSizeDiagnosticId,
                NTFabButtonConfigurationAnalyzer.InvalidPlacementDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabButton>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(2, "AriaLabel", "Create item");
        builder.AddAttribute(3, "ButtonSize", global::NTComponents.Size.Small);
        builder.AddAttribute(4, "Placement", global::NTComponents.NTFabButtonPlacement.LowerRight);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabButton>(5);
        builder.AddAttribute(6, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(7, "Label", "Create");
        builder.AddAttribute(8, "BackgroundColor", global::NTComponents.TnTColor.PrimaryContainer);
        builder.AddAttribute(9, "TextColor", global::NTComponents.TnTColor.OnPrimaryContainer);
        builder.AddAttribute(10, "ButtonSize", global::NTComponents.Size.Large);
        builder.AddAttribute(11, "Placement", global::NTComponents.NTFabButtonPlacement.Inline);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabButtonFactory.cs", source));

        Assert.Empty(diagnostics);
    }

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
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new NTFabButtonConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTFabButton { }
    public class TnTIcon { }
    public enum TnTColor { None, Transparent, PrimaryContainer, OnPrimaryContainer }
    public enum Size { Smallest, XS = Smallest, Small, Medium, Large, Largest, XL = Largest }
    public enum NTFabButtonPlacement { Inline, LowerRight, LowerLeft, UpperRight, UpperLeft }
}
""";
}
