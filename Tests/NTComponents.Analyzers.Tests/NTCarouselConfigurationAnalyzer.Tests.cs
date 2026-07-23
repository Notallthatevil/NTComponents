using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTCarouselConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Invalid_Carousel_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class CarouselFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTCarousel>(0);
        builder.AddAttribute(1, "AutoPlayInterval", 0d);
        builder.AddAttribute(2, "ItemHeight", -1);
        builder.AddAttribute(3, "MaxLargeItemWidth", -5);
        builder.AddAttribute(4, "PreferredItemWidth", 0);
        builder.AddAttribute(5, "Appearance", global::NTComponents.CarouselAppearance.FullScreen);
        builder.AddAttribute(6, "EnableSnapping", false);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarousel>(7);
        builder.AddAttribute(8, "AriaLabel", "Featured places");
        builder.AddAttribute(9, "Appearance", (global::NTComponents.CarouselAppearance)99);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarousel>(10);
        builder.AddAttribute(11, "AriaLabel", "Featured places");
        builder.AddAttribute(12, "AutoPlayInterval", double.NaN);
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
    public class NTCarousel { }
    public class NTCarouselItem { }
    public enum CarouselAppearance { MultiBrowse, Uncontained, UncontainedMultiAspectRatio, Hero, CenterAlignedHero, FullScreen }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("CarouselFactory.cs", source));

        Assert.Equal(
            [
                NTCarouselConfigurationAnalyzer.EmptyCarouselAriaLabelDiagnosticId,
                NTCarouselConfigurationAnalyzer.NonPositiveCarouselValueDiagnosticId,
                NTCarouselConfigurationAnalyzer.NonPositiveCarouselValueDiagnosticId,
                NTCarouselConfigurationAnalyzer.NonPositiveCarouselValueDiagnosticId,
                NTCarouselConfigurationAnalyzer.NonPositiveCarouselValueDiagnosticId,
                NTCarouselConfigurationAnalyzer.FullScreenSnappingDiagnosticId,
                NTCarouselConfigurationAnalyzer.UndefinedAppearanceDiagnosticId,
                NTCarouselConfigurationAnalyzer.NonPositiveCarouselValueDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.GetMessage().Contains("AutoPlayInterval", StringComparison.Ordinal));
        Assert.Contains(diagnostics, static diagnostic => diagnostic.GetMessage().Contains("PreferredItemWidth", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reports_Invalid_Carousel_Item_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class CarouselItemFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTCarouselItem>(0);
        builder.AddAttribute(1, "AspectRatio", 0.5d);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(2);
        builder.AddAttribute(3, "AriaLabel", "   ");
        builder.AddAttribute(4, "AspectRatio", 2d);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(5);
        builder.AddAttribute(6, "AriaLabel", "Infinite");
        builder.AddAttribute(7, "AspectRatio", double.PositiveInfinity);
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
    public class NTCarousel { }
    public class NTCarouselItem { }
    public enum CarouselAppearance { MultiBrowse, Uncontained, UncontainedMultiAspectRatio, Hero, CenterAlignedHero, FullScreen }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("CarouselItemFactory.cs", source));

        Assert.Equal(
            [
                NTCarouselConfigurationAnalyzer.EmptyItemAriaLabelDiagnosticId,
                NTCarouselConfigurationAnalyzer.InvalidItemAspectRatioDiagnosticId,
                NTCarouselConfigurationAnalyzer.EmptyItemAriaLabelDiagnosticId,
                NTCarouselConfigurationAnalyzer.InvalidItemAspectRatioDiagnosticId,
                NTCarouselConfigurationAnalyzer.InvalidItemAspectRatioDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Supports_Razor_TypeCheck_And_AddComponentParameter() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class GeneratedCarousel {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTCarousel>(0);
        builder.AddComponentParameter(1, "AriaLabel", RuntimeHelpers.TypeCheck<string>("Carousel"));
        builder.AddComponentParameter(2, "Appearance", RuntimeHelpers.TypeCheck<global::NTComponents.CarouselAppearance>(global::NTComponents.CarouselAppearance.FullScreen));
        builder.AddComponentParameter(3, "EnableSnapping", RuntimeHelpers.TypeCheck<bool>(false));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(4);
        builder.AddComponentParameter(5, "AriaLabel", RuntimeHelpers.TypeCheck<string>("Slide"));
        builder.AddComponentParameter(6, "AspectRatio", RuntimeHelpers.TypeCheck<double?>(4d));
        builder.CloseComponent();
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public class NTCarousel { }
    public class NTCarouselItem { }
    public enum CarouselAppearance { MultiBrowse, Uncontained, UncontainedMultiAspectRatio, Hero, CenterAlignedHero, FullScreen }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("GeneratedCarousel.razor.g.cs", source));

        Assert.Equal(
            [
                NTCarouselConfigurationAnalyzer.FullScreenSnappingDiagnosticId,
                NTCarouselConfigurationAnalyzer.InvalidItemAspectRatioDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Does_Not_Report_Valid_Or_Dynamic_Configuration() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class CarouselFactory {
    public static void Build(RenderTreeBuilder builder, string label, double interval, int size, double ratio, bool snapping, global::NTComponents.CarouselAppearance appearance) {
        builder.OpenComponent<global::NTComponents.NTCarousel>(0);
        builder.AddAttribute(1, "AriaLabel", "Featured places");
        builder.AddAttribute(2, "AutoPlayInterval", 4d);
        builder.AddAttribute(3, "ItemHeight", 240);
        builder.AddAttribute(4, "MaxLargeItemWidth", 320);
        builder.AddAttribute(5, "PreferredItemWidth", 186);
        builder.AddAttribute(6, "Appearance", global::NTComponents.CarouselAppearance.FullScreen);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(7);
        builder.AddAttribute(8, "AriaLabel", "Narrow slide");
        builder.AddAttribute(9, "AspectRatio", 9d / 16d);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(10);
        builder.AddAttribute(11, "AriaLabel", "Wide slide");
        builder.AddAttribute(12, "AspectRatio", 16d / 9d);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarousel>(13);
        builder.AddAttribute(14, "AriaLabel", label);
        builder.AddAttribute(15, "AutoPlayInterval", interval);
        builder.AddAttribute(16, "ItemHeight", size);
        builder.AddAttribute(17, "Appearance", appearance);
        builder.AddAttribute(18, "EnableSnapping", snapping);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTCarouselItem>(19);
        builder.AddAttribute(20, "AriaLabel", label);
        builder.AddAttribute(21, "AspectRatio", ratio);
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
    public class NTCarousel { }
    public class NTCarouselItem { }
    public enum CarouselAppearance { MultiBrowse, Uncontained, UncontainedMultiAspectRatio, Hero, CenterAlignedHero, FullScreen }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("CarouselFactory.cs", source));

        Assert.Empty(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(params (string Path, string Source)[] sources) {
        var syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source.Source, new CSharpParseOptions(LanguageVersion.Latest), source.Path))
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

        return await compilation
            .WithAnalyzers([new NTCarouselConfigurationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}
