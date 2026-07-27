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

    [Fact]
    public async Task Reports_Only_Statically_Out_Of_Range_Progress_Values() {
        const string source = """
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class ProgressFactory {
    public static void Build(RenderTreeBuilder builder, double runtimeMax, double runtimeValue, string runtimeName) {
        builder.OpenComponent<global::NTComponents.OtherComponent>(0);
        builder.AddAttribute(1, "Max", -1);
        builder.CloseComponent();

        builder.OpenComponent(2, typeof(global::NTComponents.NTProgress));
        builder.AddComponentParameter(3, "Max", RuntimeHelpers.TypeCheck((double)200));
        builder.AddComponentParameter(4, "Value", (double)200);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(5);
        builder.AddAttribute(6, "Value", 101);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(7);
        builder.AddAttribute(8, "Max", runtimeMax);
        builder.AddAttribute(9, "Value", runtimeValue);
        builder.AddAttribute(10, runtimeName, -1);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTProgress>(11);
        builder.AddAttribute(12, "Max", 50);
        builder.CloseComponent();
    }
}
""" + ExtendedSupportTypes;

        var diagnostic = Assert.Single(await GetDiagnosticsAsync(("ProgressFactory.cs", source)));

        Assert.Equal(NTProgressConfigurationAnalyzer.OutOfRangeValueDiagnosticId, diagnostic.Id);
        Assert.Contains("clamped", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_Only_Loader_Configurations_With_Observable_Fallbacks() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class LoaderFactory {
    public static void Build(RenderTreeBuilder builder, double runtimeMilliseconds, bool runtimeAnimate, global::NTComponents.NTShapeType[] runtimeShapes) {
        builder.OpenComponent<global::NTComponents.NTLoader>(0);
        builder.AddAttribute(1, "AnimationDuration", new TimeSpan(0, 0, 0));
        builder.AddAttribute(2, "Animate", true);
        builder.AddAttribute(3, "Shapes", RuntimeHelpers.TypeCheck(new global::NTComponents.NTShapeType[] { global::NTComponents.NTShapeType.Hexagon }));
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(4);
        builder.AddAttribute(5, "AnimationDuration", new TimeSpan(0, 0, 1));
        builder.AddAttribute(6, "Shapes", new global::NTComponents.NTShapeType[1]);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(7);
        builder.AddAttribute(8, "AnimationDuration", TimeSpan.FromMilliseconds(runtimeMilliseconds));
        builder.AddAttribute(9, "Animate", runtimeAnimate);
        builder.AddAttribute(10, "Shapes", new[] { global::NTComponents.NTShapeType.Hexagon });
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(11);
        builder.AddAttribute(12, "AnimationDuration", TimeSpan.FromSeconds(0.1));
        builder.AddAttribute(13, "Shapes", runtimeShapes);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTLoader>(14);
        builder.AddAttribute(15, "AnimationDuration", new TimeSpan());
        builder.AddAttribute(16, "Animate", false);
        builder.AddAttribute(17, "Shapes", new[] { global::NTComponents.NTShapeType.Hexagon });
        builder.CloseComponent();
    }
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("LoaderFactory.cs", source));

        Assert.Equal(1, diagnostics.Count(static diagnostic => diagnostic.Id == NTProgressConfigurationAnalyzer.ShortLoaderAnimationDiagnosticId));
        Assert.Equal(2, diagnostics.Count(static diagnostic => diagnostic.Id == NTProgressConfigurationAnalyzer.SingleShapeLoaderDiagnosticId));
        Assert.Equal(3, diagnostics.Length);
    }

    [Fact]
    public async Task Analyzes_All_Block_Executable_Forms_Without_Duplicating_Nested_Diagnostics() {
        const string source = """
using System;
using Microsoft.AspNetCore.Components.Rendering;

public sealed class LoaderFactory {
    public LoaderFactory(RenderTreeBuilder builder) {
        AddShortLoader(builder);

        void AddShortLoader(RenderTreeBuilder nestedBuilder) {
            nestedBuilder.OpenComponent(0, typeof(global::NTComponents.NTLoader));
            nestedBuilder.AddAttribute(1, "AnimationDuration", TimeSpan.FromMilliseconds(100));
            nestedBuilder.CloseComponent();
        }

        Action<int> parenthesized = (value) => {
            builder.OpenComponent<global::NTComponents.NTLoader>(2);
            builder.AddAttribute(3, "AnimationDuration", TimeSpan.FromMilliseconds(100));
            builder.CloseComponent();
        };
        Action<int> simple = value => {
            builder.OpenComponent<global::NTComponents.NTLoader>(4);
            builder.AddAttribute(5, "AnimationDuration", TimeSpan.FromMilliseconds(100));
            builder.CloseComponent();
        };
        Action anonymous = delegate {
            builder.OpenComponent<global::NTComponents.NTLoader>(6);
            builder.AddAttribute(7, "AnimationDuration", TimeSpan.FromMilliseconds(100));
            builder.CloseComponent();
        };
    }

    public int ExpressionBodied() => 42;
}
""" + ExtendedSupportTypes;

        var diagnostics = await GetDiagnosticsAsync(("LoaderFactory.cs", source));

        Assert.Equal(4, diagnostics.Count(static diagnostic => diagnostic.Id == NTProgressConfigurationAnalyzer.ShortLoaderAnimationDiagnosticId));
    }

    [Fact]
    public async Task Does_Not_Report_When_Progress_And_Loader_Types_Are_Missing() {
        const string noTypes = "public static class Factory { public static void Build() { } }";
        const string onlyProgress = "namespace NTComponents { public class NTProgress { } }";

        Assert.Empty(await GetDiagnosticsAsync(("NoTypes.cs", noTypes)));
        Assert.Empty(await GetDiagnosticsAsync(("OnlyProgress.cs", onlyProgress)));
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

    private const string ExtendedSupportTypes = """

namespace Microsoft.AspNetCore.Components.Rendering {
    public class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void OpenComponent(int sequence, System.Type componentType) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void AddComponentParameter(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace Microsoft.AspNetCore.Components.CompilerServices {
    public static class RuntimeHelpers {
        public static T TypeCheck<T>(T value) => value;
    }
}

namespace NTComponents {
    public class NTProgress { }
    public class NTLoader { }
    public class OtherComponent { }
    public enum NTShapeType { Hexagon, Oval }
}
""";
}
