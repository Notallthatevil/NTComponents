using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTSplitButtonConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Invalid_SplitButton_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTSplitButton>(0);
        builder.AddAttribute(1, "Label", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(2);
        builder.AddAttribute(3, "LeadingIcon", new global::NTComponents.TnTIcon());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(4);
        builder.AddAttribute(5, "Label", "Create");
        builder.AddAttribute(6, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(7, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(8, "TextColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(9, "MenuBackgroundColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(10, "MenuTextColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(11);
        builder.AddAttribute(12, "Label", "Save");
        builder.AddAttribute(13, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(14, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(15);
        builder.AddAttribute(16, "Label", "Export");
        builder.AddAttribute(17, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(18, "Elevation", global::NTComponents.NTElevation.None);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.cs", source));

        Assert.Equal(
            [
                NTSplitButtonConfigurationAnalyzer.EmptyLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingActionAriaLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleMenuColorDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleMenuColorDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_DividerOnly_ChildContent_As_Missing_MenuItem() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTSplitButton>(0);
        builder.AddAttribute(1, "Label", "Save");
        builder.AddAttribute(2, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTSplitButtonDividerItem>(3);
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("SplitButtonFactory.cs", source)));

        Assert.Equal(NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task Reports_Invalid_MenuItem_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(0);
        builder.AddAttribute(1, "Label", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButtonAnchorItem>(2);
        builder.AddAttribute(3, "Href", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButtonAnchorItem>(4);
        builder.AddAttribute(5, "Label", "Docs");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.cs", source));

        Assert.Equal(
            [
                NTSplitButtonConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTSplitButton>(0);
        builder.AddAttribute(1, "Label", "Save");
        builder.AddAttribute(2, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.SurfaceContainerLow);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(5, "MenuBackgroundColor", global::NTComponents.TnTColor.SurfaceContainer);
        builder.AddAttribute(6, "MenuTextColor", global::NTComponents.TnTColor.OnSurface);
        builder.AddAttribute(7, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.AddAttribute(8, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(9);
            builder2.AddAttribute(10, "Label", "Save draft");
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTSplitButtonAnchorItem>(11);
            builder2.AddAttribute(12, "Label", "Open docs");
            builder2.AddAttribute(13, "Href", "/docs");
            builder2.CloseComponent();
        }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(14);
        builder.AddAttribute(15, "LeadingIcon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(16, "ActionAriaLabel", "Create");
        builder.AddAttribute(17, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder3 => {
            builder3.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(18);
            builder3.AddAttribute(19, "Label", "Create draft");
            builder3.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.cs", source));

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

        var analyzer = new NTSplitButtonConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components {
    public delegate void RenderFragment(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTSplitButton { }
    public class NTSplitButtonButtonItem { }
    public class NTSplitButtonAnchorItem { }
    public class NTSplitButtonDividerItem { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, SurfaceContainer, OnSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";
}
