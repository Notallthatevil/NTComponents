using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTButtonGroupConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Invalid_ButtonGroup_Parameters() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "SelectedTextColor", global::NTComponents.TnTColor.None);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(5);
        builder.AddAttribute(6, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(7, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(8, "SelectedBackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(9);
        builder.AddAttribute(10, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(11, "SelectedBackgroundColor", global::NTComponents.TnTColor.None);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(
            [
                NTButtonGroupConfigurationAnalyzer.TextSelectableDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.OpaqueBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentSelectedBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.TransparentSelectedBackgroundDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.InvisibleSelectedTextColorDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_SelectedColors_When_Group_Is_Not_Selectable() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "SelectionMode", global::NTComponents.NTButtonGroupSelectionMode.None);
        builder.AddAttribute(3, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(4, "SelectedBackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(5, "SelectedTextColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_IconOnly_Item_Without_AriaLabel() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(0);
        builder.AddAttribute(1, "Icon", new global::NTComponents.TnTIcon());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(2);
        builder.AddAttribute(3, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(4, "Label", " ");
        builder.AddAttribute(5, "AriaLabel", " ");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

        Assert.Equal(
            [
                NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId,
                NTButtonGroupConfigurationAnalyzer.MissingIconOnlyAriaLabelDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Static_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonGroupFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButtonGroup<string>>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Tonal);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.SecondaryContainer);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.OnSecondaryContainer);
        builder.AddAttribute(4, "SelectedBackgroundColor", global::NTComponents.TnTColor.Secondary);
        builder.AddAttribute(5, "SelectedTextColor", global::NTComponents.TnTColor.OnSecondary);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(6);
        builder.AddAttribute(7, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(8, "AriaLabel", "Home");
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButtonGroupItem<string>>(9);
        builder.AddAttribute(10, "Icon", new global::NTComponents.TnTIcon());
        builder.AddAttribute(11, "Label", "Home");
        builder.CloseComponent();
    }
}
""" + SupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("ButtonGroupFactory.cs", source));

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

        var analyzer = new NTButtonGroupConfigurationAnalyzer();
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
    public class NTButtonGroup<TObjectType> { }
    public class NTButtonGroupItem<TObjectType> { }
    public class TnTIcon { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum NTButtonGroupSelectionMode { Single, Multiple, None }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, SurfaceContainer, OnSurfaceVariant, InverseSurface, InverseOnSurface }
}
""";
}
