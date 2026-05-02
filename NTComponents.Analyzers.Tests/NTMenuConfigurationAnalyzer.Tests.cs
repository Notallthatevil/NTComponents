using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTMenuConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Invalid_Menu_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTMenu>(0);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenu>(1);
        builder.AddAttribute(2, "AriaLabel", " ");
        builder.AddAttribute(3, "ContainerColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "TextColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(5, "SelectedContainerColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(6, "SelectedTextColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(7, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTMenuDividerItem>(8);
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuLabelItem>(9);
            builder2.AddAttribute(10, "Label", "Document");
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

        Assert.Equal(
            [
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Invalid_MenuItem_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTMenuButtonItem>(0);
        builder.AddAttribute(1, "Label", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenuAnchorItem>(2);
        builder.AddAttribute(3, "Href", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenuAnchorItem>(4);
        builder.AddAttribute(5, "Label", "Docs");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenuSubMenuItem>(6);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenuLabelItem>(7);
        builder.AddAttribute(8, "Label", " ");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

        Assert.Equal(
            [
                NTMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.EmptyMenuItemLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId,
                NTMenuConfigurationAnalyzer.EmptyMenuItemHrefDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTMenu>(0);
        builder.AddAttribute(1, "AriaLabel", "Document actions");
        builder.AddAttribute(2, "ContainerColor", global::NTComponents.TnTColor.SurfaceContainerLow);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.OnSurface);
        builder.AddAttribute(4, "SelectedContainerColor", global::NTComponents.TnTColor.TertiaryContainer);
        builder.AddAttribute(5, "SelectedTextColor", global::NTComponents.TnTColor.OnTertiaryContainer);
        builder.AddAttribute(6, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTMenuButtonItem>(7);
            builder2.AddAttribute(8, "Label", "Save draft");
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuLabelItem>(9);
            builder2.AddAttribute(10, "Label", "Links");
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuAnchorItem>(11);
            builder2.AddAttribute(12, "Label", "Open docs");
            builder2.AddAttribute(13, "Href", "/docs");
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuSubMenuItem>(14);
            builder2.AddAttribute(15, "Label", "More actions");
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

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

        var analyzer = new NTMenuConfigurationAnalyzer();
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
    public class NTMenu { }
    public class NTMenuButtonItem { }
    public class NTMenuAnchorItem { }
    public class NTMenuDividerItem { }
    public class NTMenuLabelItem { }
    public class NTMenuSubMenuItem { }
    public enum TnTColor { None, Transparent, SurfaceContainerLow, OnSurface, TertiaryContainer, OnTertiaryContainer }
}
""";
}
