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

    [Fact]
    public async Task Reports_Required_Contracts_From_NonGeneric_And_Nested_Executable_Shapes() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class SplitButtonFactory {
    public SplitButtonFactory(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTSplitButton));
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local() {
            builder.OpenComponent<global::NTComponents.NTSplitButton>(1);
            builder.CloseComponent();
        }

        Action<RenderTreeBuilder> parenthesized = (nestedBuilder) => {
            nestedBuilder.OpenComponent<global::NTComponents.NTSplitButton>(2);
            nestedBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> simple = nestedBuilder => {
            nestedBuilder.OpenComponent<global::NTComponents.NTSplitButton>(3);
            nestedBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> anonymous = delegate(RenderTreeBuilder nestedBuilder) {
            nestedBuilder.OpenComponent<global::NTComponents.NTSplitButton>(4);
            nestedBuilder.CloseComponent();
        };
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.razor.g.cs", source));

        Assert.Equal(10, diagnostics.Length);
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTSplitButtonConfigurationAnalyzer.EmptyLabelDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTSplitButtonConfigurationAnalyzer.MissingMenuItemDiagnosticId));
        Assert.All(diagnostics, static diagnostic => Assert.Equal("SplitButtonFactory.razor.g.cs", diagnostic.Location.GetLineSpan().Path));
    }

    [Fact]
    public async Task Reports_Exact_Diagnostics_For_Razor_TypeChecked_Constant_Casts() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTSplitButton));
        builder.AddComponentParameter(1, "Label", RuntimeHelpers.TypeCheck<string>("Save"));
        builder.AddComponentParameter(2, "Variant", RuntimeHelpers.TypeCheck<global::NTComponents.NTButtonVariant>((global::NTComponents.NTButtonVariant)4));
        builder.AddComponentParameter(3, "BackgroundColor", RuntimeHelpers.TypeCheck<global::NTComponents.TnTColor?>(((global::NTComponents.TnTColor)2)));
        builder.AddComponentParameter(4, "TextColor", RuntimeHelpers.TypeCheck<global::NTComponents.TnTColor>((global::NTComponents.TnTColor)1));
        builder.AddComponentParameter(5, "MenuBackgroundColor", RuntimeHelpers.TypeCheck<global::NTComponents.TnTColor>((global::NTComponents.TnTColor)0));
        builder.AddComponentParameter(6, "MenuTextColor", RuntimeHelpers.TypeCheck<global::NTComponents.TnTColor>((global::NTComponents.TnTColor)1));
        builder.AddComponentParameter(7, "Elevation", RuntimeHelpers.TypeCheck<global::NTComponents.NTElevation>((global::NTComponents.NTElevation)1));
        builder.AddComponentParameter(8, "ChildContent", RuntimeHelpers.TypeCheck<global::Microsoft.AspNetCore.Components.RenderFragment>(nestedBuilder => {
            nestedBuilder.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(9);
            nestedBuilder.AddComponentParameter(10, "Label", RuntimeHelpers.TypeCheck<string>("Save draft"));
            nestedBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.razor.g.cs", source));

        Assert.Equal(
            [
                NTSplitButtonConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleMenuColorDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.InvisibleMenuColorDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.GetMessage() == "NTSplitButton variant 'Text' must use a transparent BackgroundColor");
        Assert.Contains(diagnostics, static diagnostic => diagnostic.GetMessage() == "NTSplitButton MenuBackgroundColor must be a visible menu color");
    }

    [Fact]
    public async Task Distinguishes_Static_Invalid_Values_From_Dynamic_And_Unknown_Content() {
        const string source = """
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public static class SplitButtonFactory {
    public static void Build(RenderTreeBuilder builder, string label, global::NTComponents.NTButtonVariant variant, global::NTComponents.TnTColor color, global::NTComponents.NTElevation elevation, RenderFragment content, string attributeName) {
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Variant", variant);
        builder.AddAttribute(3, "BackgroundColor", color);
        builder.AddAttribute(4, "Elevation", elevation);
        builder.AddAttribute(5, "ChildContent", content);
        builder.AddAttribute(6, attributeName, color);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(7);
        builder.AddAttribute(8, "LeadingIcon", null);
        builder.AddAttribute(9, "ChildContent", (RenderFragment)(nestedBuilder => {
            nestedBuilder.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(10);
            nestedBuilder.AddAttribute(11, "Label", "Action");
            nestedBuilder.CloseComponent();
        }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(12);
        builder.AddAttribute(13, "LeadingIcon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(14, "ActionAriaLabel", null);
        builder.AddAttribute(15, "ChildContent", content);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(16);
        builder.AddAttribute(17, "Label", "Tonal");
        builder.AddAttribute(18, "Variant", global::NTComponents.NTButtonVariant.Tonal);
        builder.AddAttribute(19, "BackgroundColor", global::NTComponents.TnTColor.None);
        builder.AddAttribute(20, "Elevation", global::NTComponents.NTElevation.None);
        builder.AddAttribute(21, "ChildContent", content);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(22);
        builder.AddAttribute(23, "Label", "Outlined");
        builder.AddAttribute(24, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(25, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(26, "Elevation", global::NTComponents.NTElevation.None);
        builder.AddAttribute(27, "ChildContent", (RenderFragment)(nestedBuilder => nestedBuilder.CloseComponent()));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButtonButtonItem>(28);
        builder.AddAttribute(29, "Label", label);
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.NTSplitButtonAnchorItem>(30);
        builder.AddAttribute(31, "Label", label);
        builder.AddAttribute(32, "Href", label);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.OtherComponent>(33);
        builder.AddAttribute(34, "Label", " ");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTSplitButton>(35);
        builder.AddAttribute(36, "Label", "Incomplete");
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("SplitButtonFactory.cs", source));

        Assert.Equal(
            [
                NTSplitButtonConfigurationAnalyzer.EmptyLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.MissingActionAriaLabelDiagnosticId,
                NTSplitButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_When_SplitButton_Contracts_Are_Incomplete() {
        const string source = """
namespace NTComponents {
    public class NTSplitButton { }
}

public static class ApplicationCode {
    public static void Run() { }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ApplicationCode.cs", source));

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

        Assert.DoesNotContain(compilation.GetDiagnostics(TestContext.Current.CancellationToken), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var analyzer = new NTSplitButtonConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }

    private const string SupportTypes = """

namespace Microsoft.AspNetCore.Components {
    public delegate void RenderFragment(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

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
    public class NTSplitButton { }
    public class NTSplitButtonButtonItem { }
    public class NTSplitButtonAnchorItem { }
    public class NTSplitButtonDividerItem { }
    public class OtherComponent { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, SurfaceContainer, OnSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";
}
