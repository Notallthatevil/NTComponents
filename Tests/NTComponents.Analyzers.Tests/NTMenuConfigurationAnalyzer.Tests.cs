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

    [Fact]
    public async Task Reports_Invalid_ContextMenu_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ContextMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTContextMenu>(0);
        builder.AddAttribute(1, "TargetContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => { }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTContextMenu>(2);
        builder.AddAttribute(3, "AriaLabel", " ");
        builder.AddAttribute(4, "MenuContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTMenuDividerItem>(5);
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuLabelItem>(6);
            builder2.AddAttribute(7, "Label", "Row actions");
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ContextMenuFactory.cs", source));

        Assert.Equal(
            [
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_ContextMenu_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ContextMenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTContextMenu>(0);
        builder.AddAttribute(1, "AriaLabel", "Row actions");
        builder.AddAttribute(2, "TargetContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => { }));
        builder.AddAttribute(3, "MenuContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(builder2 => {
            builder2.OpenComponent<global::NTComponents.NTMenuButtonItem>(4);
            builder2.AddAttribute(5, "Label", "Rename");
            builder2.CloseComponent();

            builder2.OpenComponent<global::NTComponents.NTMenuAnchorItem>(6);
            builder2.AddAttribute(7, "Label", "Open details");
            builder2.AddAttribute(8, "Href", "/details");
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ContextMenuFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_Invalid_NonGeneric_Menu_With_Generated_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTMenu));
        builder.AddComponentParameter(1, "AriaLabel", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>(((" "))));
        builder.AddComponentParameter(2, "ContainerColor", (global::NTComponents.TnTColor)0);
        builder.AddComponentParameter(3, "TextColor", (global::NTComponents.TnTColor)1);
        builder.AddComponentParameter(4, "SelectedContainerColor", (global::NTComponents.TnTColor)0);
        builder.AddComponentParameter(5, "SelectedTextColor", (global::NTComponents.TnTColor)1);
        builder.AddComponentParameter(6, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)(itemBuilder => {
            itemBuilder.OpenComponent<global::NTComponents.NTMenuButtonItem>(7);
            itemBuilder.AddAttribute(8, "Label", "Save");
            itemBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

        Assert.Equal(
            [
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId,
                NTMenuConfigurationAnalyzer.InvisibleColorDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Missing_Menu_Requirements_In_Each_Supported_Executable_Body() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class MenuFactory {
    public MenuFactory(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTMenu>(0);
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local(RenderTreeBuilder localBuilder) {
            localBuilder.OpenComponent<global::NTComponents.NTMenu>(0);
            localBuilder.CloseComponent();
        }

        Action<RenderTreeBuilder> parenthesized = (lambdaBuilder) => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTMenu>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> simple = lambdaBuilder => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTMenu>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> anonymous = delegate(RenderTreeBuilder anonymousBuilder) {
            anonymousBuilder.OpenComponent<global::NTComponents.NTMenu>(0);
            anonymousBuilder.CloseComponent();
        };
    }

    public static void ExpressionBodied(RenderTreeBuilder builder) => builder.Noop();
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId));
    }

    [Fact]
    public async Task Reports_Missing_Menu_Requirements_When_Attribute_Names_Are_Dynamic() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder, string attributeName) {
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.OtherComponent>(0);
        builder.AddAttribute(1, "AriaLabel", "Ignored");
        builder.CloseComponent();

        var componentType = typeof(global::NTComponents.NTMenu);
        builder.OpenComponent(2, componentType);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenu>(3);
        builder.AddAttribute(4, attributeName, "Actions");
        builder.Noop();
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("MenuFactory.cs", source));

        Assert.Equal(
            [
                NTMenuConfigurationAnalyzer.MissingAriaLabelDiagnosticId,
                NTMenuConfigurationAnalyzer.MissingMenuItemDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_When_Menu_Content_Or_Values_Are_Runtime_Dependent() {
        const string source = """
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class MenuFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTMenu>(0);
        builder.AddAttribute(1, "AriaLabel", GetText());
        builder.AddAttribute(2, "ContainerColor", GetColor());
        builder.AddAttribute(3, "ChildContent", GetContent());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenu>(4);
        builder.AddAttribute(5, "AriaLabel", "Dynamic actions");
        builder.AddAttribute(6, "ChildContent", (RenderFragment)(itemBuilder => {
            itemBuilder.AddContent(7, GetContent());
        }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenu>(8);
        builder.AddAttribute(9, "AriaLabel", "Custom content");
        builder.AddAttribute(10, "ChildContent", (RenderFragment)(itemBuilder => {
            itemBuilder.OpenComponent<global::NTComponents.OtherComponent>(11);
            itemBuilder.CloseComponent();
        }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTMenuAnchorItem>(12);
        builder.AddAttribute(13, "Label", GetText());
        builder.AddAttribute(14, "Href", GetText());
        builder.CloseComponent();
    }

    private static string GetText() => "Actions";
    private static global::NTComponents.TnTColor GetColor() => global::NTComponents.TnTColor.OnSurface;
    private static RenderFragment GetContent() => itemBuilder => { };
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
        public void OpenComponent(int sequence, global::System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void AddContent(int sequence, global::Microsoft.AspNetCore.Components.RenderFragment content) { }
        public void CloseComponent() { }
        public void Noop() { }
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public class NTMenu { }
    public class NTContextMenu { }
    public class NTMenuButtonItem { }
    public class NTMenuAnchorItem { }
    public class NTMenuDividerItem { }
    public class NTMenuLabelItem { }
    public class NTMenuSubMenuItem { }
    public class OtherComponent { }
    public enum TnTColor { None, Transparent, SurfaceContainerLow, OnSurface, TertiaryContainer, OnTertiaryContainer }
}
""";
}
