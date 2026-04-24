using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTButtonConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_TextButton_With_VisibleBackground_And_Toggle() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButton>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(3, "IsToggleButton", true);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ButtonFactory.cs", source));

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => Assert.Equal(NTButtonConfigurationAnalyzer.OpaqueBackgroundDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(NTButtonConfigurationAnalyzer.TextToggleDiagnosticId, diagnostic.Id));
    }

    [Theory]
    [InlineData("Elevated")]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    public async Task Reports_ContainedButton_With_TransparentBackground(string variant) {
        var source = $$"""
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButton>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.{{variant}});
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ButtonFactory.cs", source));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NTButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task Reports_InvisibleTextColor_EmptyLabel_And_InvalidElevations() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButton>(0);
        builder.AddAttribute(1, "Label", " ");
        builder.AddAttribute(2, "TextColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(3, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButton>(4);
        builder.AddAttribute(5, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(6, "Elevation", global::NTComponents.NTElevation.None);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ButtonFactory.cs", source));

        Assert.Equal(
            [
                NTButtonConfigurationAnalyzer.InvisibleTextColorDiagnosticId,
                NTButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTButtonConfigurationAnalyzer.InvalidElevationDiagnosticId,
                NTButtonConfigurationAnalyzer.EmptyLabelDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReport_For_Valid_Button_Combinations() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButton>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Text);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.AddAttribute(3, "TextColor", global::NTComponents.TnTColor.Primary);
        builder.AddAttribute(4, "Elevation", global::NTComponents.NTElevation.None);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButton>(5);
        builder.AddAttribute(6, "Variant", global::NTComponents.NTButtonVariant.Elevated);
        builder.AddAttribute(7, "BackgroundColor", global::NTComponents.TnTColor.SurfaceContainerLow);
        builder.AddAttribute(8, "Elevation", global::NTComponents.NTElevation.Lowest);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTButton>(9);
        builder.AddAttribute(10, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(11, "IsToggleButton", true);
        builder.AddAttribute(12, "Selected", true);
        builder.AddAttribute(13, "BackgroundColor", global::NTComponents.TnTColor.InverseSurface);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ButtonFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Reports_SelectedOutlinedToggle_With_TransparentBackground() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTButton>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTButtonVariant.Outlined);
        builder.AddAttribute(2, "IsToggleButton", true);
        builder.AddAttribute(3, "Selected", true);
        builder.AddAttribute(4, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("ButtonFactory.cs", source)));

        Assert.Equal(NTButtonConfigurationAnalyzer.TransparentBackgroundDiagnosticId, diagnostic.Id);
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

        var analyzer = new NTButtonConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }
}
