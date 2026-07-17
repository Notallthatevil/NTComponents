using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTCardConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_TransparentBackground_In_RazorGeneratedCode_WithMappedRazorPath() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent {
    public void Build(RenderTreeBuilder __builder) {
#line 3 "TestComponent.razor"
        __builder.OpenComponent<global::NTComponents.NTCard>(0);
        __builder.AddAttribute(1, "BackgroundColor", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::NTComponents.TnTColor?>(global::NTComponents.TnTColor.Transparent));
        __builder.CloseComponent();
#line default
#line hidden
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public class NTCard { }
    public enum NTCardVariant { Filled, Outlined, Elevated }
    public enum TnTColor { None, Transparent, SurfaceContainerHighest, SurfaceContainerLow, OnSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("TestComponent.razor.g.cs", source));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NTCardConfigurationAnalyzer.TransparentBackgroundDiagnosticId, diagnostic.Id);
        Assert.Equal("TestComponent.razor", diagnostic.Location.GetMappedLineSpan().Path);
    }

    [Fact]
    public async Task Reports_IgnoredElevation_In_RenderFragmentBuilderCode() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class CardFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTCard>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTCardVariant.Filled);
        builder.AddAttribute(2, "Elevation", global::NTComponents.NTElevation.Highest);
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
    public class NTCard { }
    public enum NTCardVariant { Filled, Outlined, Elevated }
    public enum TnTColor { None, Transparent, SurfaceContainerHighest, SurfaceContainerLow, OnSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("CardFactory.cs", source));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NTCardConfigurationAnalyzer.IgnoredElevationDiagnosticId, diagnostic.Id);
        Assert.Equal("CardFactory.cs", diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public async Task DoesNotReport_For_OutlinedTransparentBackground_And_ElevatedElevation() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class CardFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTCard>(0);
        builder.AddAttribute(1, "Variant", global::NTComponents.NTCardVariant.Outlined);
        builder.AddAttribute(2, "BackgroundColor", global::NTComponents.TnTColor.Transparent);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCard>(3);
        builder.AddAttribute(4, "Variant", global::NTComponents.NTCardVariant.Elevated);
        builder.AddAttribute(5, "Elevation", global::NTComponents.NTElevation.High);
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
    public class NTCard { }
    public enum NTCardVariant { Filled, Outlined, Elevated }
    public enum TnTColor { None, Transparent, SurfaceContainerHighest, SurfaceContainerLow, OnSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("CardFactory.cs", source));

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

        var analyzer = new NTCardConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }
}
