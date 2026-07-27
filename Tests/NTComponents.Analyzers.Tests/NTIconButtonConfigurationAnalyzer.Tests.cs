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

    [Fact]
    public async Task Reports_Invalid_NonGeneric_Component_With_Generated_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTIconButton));
        builder.AddComponentParameter(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddComponentParameter(2, "AriaLabel", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>(((" "))));
        builder.AddComponentParameter(3, "Variant", (global::NTComponents.NTButtonVariant)3);
        builder.AddComponentParameter(4, "BackgroundColor", (global::NTComponents.TnTColor)0);
        builder.AddComponentParameter(5, "TextColor", (global::NTComponents.TnTColor)0);
        builder.AddComponentParameter(6, "Elevation", (global::NTComponents.NTElevation)1);
        builder.AddAttribute(7, "IsToggleButton", true);
        builder.AddAttribute(8, "Selected", true);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

        Assert.Equal(
            [
                NTIconButtonConfigurationAnalyzer.EmptyAriaLabelDiagnosticId,
                NTIconButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTIconButtonConfigurationAnalyzer.InvalidElevationDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Missing_Requirements_In_Each_Supported_Executable_Body() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class IconButtonFactory {
    public IconButtonFactory(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTIconButton>(0);
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local(RenderTreeBuilder localBuilder) {
            localBuilder.OpenComponent<global::NTComponents.NTIconButton>(0);
            localBuilder.CloseComponent();
        }

        Action<RenderTreeBuilder> parenthesized = (lambdaBuilder) => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTIconButton>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> simple = lambdaBuilder => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTIconButton>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> anonymous = delegate(RenderTreeBuilder anonymousBuilder) {
            anonymousBuilder.OpenComponent<global::NTComponents.NTIconButton>(0);
            anonymousBuilder.CloseComponent();
        };
    }

    public static void ExpressionBodied(RenderTreeBuilder builder) => builder.Noop();
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTIconButtonConfigurationAnalyzer.MissingIconDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTIconButtonConfigurationAnalyzer.EmptyAriaLabelDiagnosticId));
    }

    [Fact]
    public async Task Reports_Missing_Requirements_When_Attribute_Names_Are_Dynamic() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder, string attributeName) {
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.OtherComponent>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.CloseComponent();

        var componentType = typeof(global::NTComponents.NTIconButton);
        builder.OpenComponent(2, componentType);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(3);
        builder.AddAttribute(4, attributeName, new global::NTComponents.TnTIcon());
        builder.Noop();
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("IconButtonFactory.cs", source));

        Assert.Equal(
            [
                NTIconButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTIconButtonConfigurationAnalyzer.EmptyAriaLabelDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_When_Component_Values_Are_Runtime_Dependent() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class IconButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTIconButton>(0);
        builder.AddAttribute(1, "Icon", GetIcon());
        builder.AddAttribute(2, "AriaLabel", GetLabel());
        builder.AddAttribute(3, "Variant", GetVariant());
        builder.AddAttribute(4, "BackgroundColor", GetColor());
        builder.AddAttribute(5, "TextColor", GetColor());
        builder.AddAttribute(6, "Elevation", GetElevation());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTIconButton>(7);
        builder.AddAttribute(8, "Icon", GetIcon());
        builder.AddAttribute(9, "AriaLabel", "Favorite");
        builder.AddAttribute(10, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(11, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(12, "IsToggleButton", false);
        builder.AddAttribute(13, "Selected", GetBoolean());
        builder.CloseComponent();
    }

    private static global::NTComponents.TnTIcon GetIcon() => new();
    private static string GetLabel() => "Open menu";
    private static global::NTComponents.NTButtonVariant GetVariant() => global::NTComponents.NTButtonVariant.Text;
    private static global::NTComponents.TnTColor GetColor() => global::NTComponents.TnTColor.Transparent;
    private static global::NTComponents.NTElevation GetElevation() => global::NTComponents.NTElevation.None;
    private static bool GetBoolean() => false;
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
        public void OpenComponent(int sequence, global::System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
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
    public class NTIconButton { }
    public class OtherComponent { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, OnSurfaceVariant, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";
}
