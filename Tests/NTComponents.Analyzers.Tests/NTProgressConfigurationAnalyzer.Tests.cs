using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTProgressConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Progress_With_NonPositive_Max_And_OutOfRange_Value() {
        const string source = """
using Microsoft.AspNetCore.Components.Rendering;

public static class ProgressFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTProgress>(0);
        builder.AddAttribute(1, "Max", 0);
        builder.AddAttribute(2, "Value", 25);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(3);
        builder.AddAttribute(4, "Max", 50);
        builder.AddAttribute(5, "Value", 75);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(6);
        builder.AddAttribute(7, "Value", -1);
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
    public class NTProgress { }
    public class NTLoader { }
    public enum NTShapeType { Hexagon, Oval }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("ProgressFactory.cs", source));

        Assert.Equal(
            [
                NTProgressConfigurationAnalyzer.NonPositiveMaxDiagnosticId,
                NTProgressConfigurationAnalyzer.OutOfRangeValueDiagnosticId,
                NTProgressConfigurationAnalyzer.OutOfRangeValueDiagnosticId
            ],
            diagnostics.Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Reports_Loader_With_Clamped_Duration_And_Single_Animated_Shape() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public static class LoaderFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTLoader>(0);
        builder.AddAttribute(1, "AnimationDuration", TimeSpan.FromMilliseconds(250));
        builder.AddAttribute(2, "Shapes", new[] { global::NTComponents.NTShapeType.Hexagon });
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
    public class NTProgress { }
    public class NTLoader { }
    public enum NTShapeType { Hexagon, Oval }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("LoaderFactory.cs", source));

        Assert.Equal(
            [
                NTProgressConfigurationAnalyzer.ShortLoaderAnimationDiagnosticId,
                NTProgressConfigurationAnalyzer.SingleShapeLoaderDiagnosticId
            ],
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).Select(static diagnostic => diagnostic.Id));
    }

    [Fact]
    public async Task Does_Not_Report_For_Valid_Progress_And_Loader_Configuration() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public static class IndicatorFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<global::NTComponents.NTProgress>(0);
        builder.AddAttribute(1, "Max", 200);
        builder.AddAttribute(2, "Value", 125);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(3);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(4);
        builder.AddAttribute(5, "AnimationDuration", TimeSpan.FromMilliseconds(900));
        builder.AddAttribute(6, "Shapes", new[] { global::NTComponents.NTShapeType.Hexagon, global::NTComponents.NTShapeType.Oval });
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(7);
        builder.AddAttribute(8, "Animate", false);
        builder.AddAttribute(9, "Shapes", new[] { global::NTComponents.NTShapeType.Hexagon });
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
    public class NTProgress { }
    public class NTLoader { }
    public enum NTShapeType { Hexagon, Oval }
}
""";

        var diagnostics = await GetDiagnosticsAsync(("IndicatorFactory.cs", source));

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

        var analyzer = new NTProgressConfigurationAnalyzer();
        return await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync();
    }
}
