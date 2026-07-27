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

    [Fact]
    public async Task Reports_Invalid_NonGeneric_Component_With_Generated_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent(0, typeof(global::NTComponents.NTFabButton));
        builder.AddComponentParameter(1, "Icon", new global::NTComponents.TnTIcon());
        builder.AddComponentParameter(2, "Label", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>((("Create\r\nitem"))));
        builder.AddComponentParameter(3, "BackgroundColor", (global::NTComponents.TnTColor)0);
        builder.AddComponentParameter(4, "TextColor", (global::NTComponents.TnTColor)1);
        builder.AddComponentParameter(5, "ButtonSize", (global::NTComponents.Size)0);
        builder.AddComponentParameter(6, "Placement", (global::NTComponents.NTFabButtonPlacement)999);
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
                NTFabButtonConfigurationAnalyzer.InvalidPlacementDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Missing_Requirements_In_Each_Supported_Executable_Body() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class FabButtonFactory {
    public FabButtonFactory(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabButton>(0);
        builder.CloseComponent();
    }

    public static void Build(RenderTreeBuilder builder) {
        void Local(RenderTreeBuilder localBuilder) {
            localBuilder.OpenComponent<global::NTComponents.NTFabButton>(0);
            localBuilder.CloseComponent();
        }

        Action<RenderTreeBuilder> parenthesized = (lambdaBuilder) => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTFabButton>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> simple = lambdaBuilder => {
            lambdaBuilder.OpenComponent<global::NTComponents.NTFabButton>(0);
            lambdaBuilder.CloseComponent();
        };
        Action<RenderTreeBuilder> anonymous = delegate(RenderTreeBuilder anonymousBuilder) {
            anonymousBuilder.OpenComponent<global::NTComponents.NTFabButton>(0);
            anonymousBuilder.CloseComponent();
        };
    }

    public static void ExpressionBodied(RenderTreeBuilder builder) => builder.Noop();
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabButtonFactory.cs", source));

        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTFabButtonConfigurationAnalyzer.MissingIconDiagnosticId));
        Assert.Equal(5, diagnostics.Count(static diagnostic => diagnostic.Id == NTFabButtonConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId));
    }

    [Fact]
    public async Task Reports_Missing_Requirements_When_Attribute_Names_Are_Dynamic() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder, string attributeName) {
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.OtherComponent>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.CloseComponent();

        var componentType = typeof(global::NTComponents.NTFabButton);
        builder.OpenComponent(2, componentType);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabButton>(3);
        builder.AddAttribute(4, attributeName, new global::NTComponents.TnTIcon());
        builder.Noop();
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("FabButtonFactory.cs", source));

        Assert.Equal(
            [
                NTFabButtonConfigurationAnalyzer.MissingIconDiagnosticId,
                NTFabButtonConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_When_Component_Values_Are_Runtime_Dependent() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class FabButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTFabButton>(0);
        builder.AddAttribute(1, "Icon", GetIcon());
        builder.AddAttribute(2, "Label", GetLabel());
        builder.AddAttribute(3, "AriaLabel", GetLabel());
        builder.AddAttribute(4, "BackgroundColor", GetColor());
        builder.AddAttribute(5, "TextColor", GetColor());
        builder.AddAttribute(6, "ButtonSize", GetSize());
        builder.AddAttribute(7, "Placement", GetPlacement());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTFabButton>(8);
        builder.AddAttribute(9, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(10, "Label", null);
        builder.AddAttribute(11, "AriaLabel", "Create item");
        builder.CloseComponent();
    }

    private static global::NTComponents.TnTIcon GetIcon() => new();
    private static string GetLabel() => "Create";
    private static global::NTComponents.TnTColor GetColor() => global::NTComponents.TnTColor.PrimaryContainer;
    private static global::NTComponents.Size GetSize() => global::NTComponents.Size.Medium;
    private static global::NTComponents.NTFabButtonPlacement GetPlacement() => global::NTComponents.NTFabButtonPlacement.Inline;
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
    public class NTFabButton { }
    public class OtherComponent { }
    public class TnTIcon { }
    public enum TnTColor { None, Transparent, PrimaryContainer, OnPrimaryContainer }
    public enum Size { Smallest, XS = Smallest, Small, Medium, Large, Largest, XL = Largest }
    public enum NTFabButtonPlacement { Inline, LowerRight, LowerLeft, UpperRight, UpperLeft }
}
""";
}
