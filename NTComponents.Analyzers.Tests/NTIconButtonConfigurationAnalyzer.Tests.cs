using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTIconButtonConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Missing_Required_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTIconButton>(0);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(1);
        builder.AddAttribute(2, "Icon", null);
        builder.AddAttribute(3, "AriaLabel", " ");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

        Assert.Equal(
            [
                NTIconButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTIconButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTIconButtonConfigurationAnalyzer.EmptyAriaLabelDiagnosticId,
                NTIconButtonConfigurationAnalyzer.EmptyAriaLabelDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Invalid_Color_And_Elevation_Combinations() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTIconButton>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(2, "AriaLabel", "Open menu");
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(5, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(6);
        builder.AddAttribute(7, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(8, "AriaLabel", "Favorite");
        builder.AddAttribute(9, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(10, "IsToggleButton", true);
        builder.AddAttribute(11, "Selected", true);
        builder.AddAttribute(12, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(13);
        builder.AddAttribute(14, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(15, "AriaLabel", "Save");
        builder.AddAttribute(16, "Variant", global::NTComponents.NTButtonVariant.Filled);
        builder.AddAttribute(17, "BackgroundColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(18, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(19);
        builder.AddAttribute(20, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(21, "AriaLabel", "Raise");
        builder.AddAttribute(22, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(23, "Elevation", global::NTComponents.NTElevation.None);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

        Assert.Equal(
            [
                NTIconButtonConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTIconButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTIconButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvalidElevationDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTIconButton>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(2, "AriaLabel", "Open menu");
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.OnSurfaceVariant);
        builder.AddAttribute(5, "Elevation", global::NTComponents.NTElevation.None);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(6);
        builder.AddAttribute(7, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(8, "AriaLabel", "Raise");
        builder.AddAttribute(9, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(10, "BackgroundColor", global::NTComponents.TnTColor.SurfaceContainerLow);
        builder.AddAttribute(11, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(12);
        builder.AddAttribute(13, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(14, "AriaLabel", "Favorite");
        builder.AddAttribute(15, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(16, "IsToggleButton", true);
        builder.AddAttribute(17, "Selected", true);
        builder.AddAttribute(18, "BackgroundColor", global::NTComponents.TnTColor.InverseSurface);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

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

        var analyzer = new NTIconButtonConfigurationAnalyzer();
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
    public class NTIconButton { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, OnSurfaceVariant, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";
}
