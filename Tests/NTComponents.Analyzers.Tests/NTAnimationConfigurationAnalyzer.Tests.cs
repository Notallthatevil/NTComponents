using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

public sealed class NTAnimationConfigurationAnalyzer_Tests {

    [Fact]
    public async Task Reports_Shown_When_Trigger_Is_Not_Manual_Or_Unset() {
        const string body = """
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Shown", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(2);
        builder.AddAttribute(3, "Trigger", global::NTComponents.NTAnimationTrigger.Render);
        builder.AddAttribute(4, "Shown", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(5);
        builder.AddAttribute(6, "Trigger", global::NTComponents.NTAnimationTrigger.Visible);
        builder.AddAttribute(7, "Shown", true);
        builder.CloseComponent();
""";

        var diagnostics = await GetDiagnosticsAsync(body);

        Assert.Equal(3, diagnostics.Count(static d => d.Id == NTAnimationConfigurationAnalyzer.ShownRequiresManualDiagnosticId));
        Assert.Equal(3, diagnostics.Length);
    }

    [Theory]
    [InlineData("Render")]
    [InlineData("Manual")]
    public async Task Reports_Each_Visible_Only_Parameter_When_Trigger_Is_Not_Visible(string trigger) {
        var body = $$"""
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Trigger", global::NTComponents.NTAnimationTrigger.{{trigger}});
        builder.AddAttribute(2, "Repeat", 1);
        builder.AddAttribute(3, "Threshold", 0.5);
        builder.AddAttribute(4, "RootMargin", "10px");
        builder.AddAttribute(5, "OnEntered", new object());
        builder.AddAttribute(6, "OnExited", new object());
        builder.CloseComponent();
""";

        var diagnostics = await GetDiagnosticsAsync(body);

        Assert.Equal(5, diagnostics.Count(static d => d.Id == NTAnimationConfigurationAnalyzer.VisibleOnlyParameterDiagnosticId));
        Assert.Equal(5, diagnostics.Length);
    }

    [Fact]
    public async Task Reports_Threshold_Outside_Zero_To_One() {
        const string body = """
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Threshold", 1.5);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(2);
        builder.AddAttribute(3, "Threshold", -0.1);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(4);
        builder.AddAttribute(5, "Threshold", double.NaN);
        builder.CloseComponent();

        builder.OpenComponent(6, typeof(global::NTComponents.NTAnimation));
        builder.AddComponentParameter(7, "Threshold", RuntimeHelpers.TypeCheck(2d));
        builder.CloseComponent();
""";

        var diagnostics = await GetDiagnosticsAsync(body);

        Assert.Equal(4, diagnostics.Count(static d => d.Id == NTAnimationConfigurationAnalyzer.ThresholdOutOfRangeDiagnosticId));
        Assert.Equal(4, diagnostics.Length);
    }

    [Fact]
    public async Task Does_Not_Report_Valid_Configurations() {
        const string body = """
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Trigger", global::NTComponents.NTAnimationTrigger.Manual);
        builder.AddAttribute(2, "Shown", true);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(3);
        builder.AddAttribute(4, "Repeat", 1);
        builder.AddAttribute(5, "Threshold", 0);
        builder.AddAttribute(6, "RootMargin", "10px");
        builder.AddAttribute(7, "OnEntered", new object());
        builder.AddAttribute(8, "OnExited", new object());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(9);
        builder.AddAttribute(10, "Trigger", global::NTComponents.NTAnimationTrigger.Visible);
        builder.AddAttribute(11, "Threshold", 0.5);
        builder.AddAttribute(12, "OnEntered", new object());
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(13);
        builder.AddAttribute(14, "Threshold", 1);
        builder.CloseComponent();

        builder.OpenComponent<global::NTComponents.NTAnimation>(15);
        builder.AddAttribute(16, "Trigger", global::NTComponents.NTAnimationTrigger.Render);
        builder.CloseComponent();
""";

        Assert.Empty(await GetDiagnosticsAsync(body));
    }

    [Fact]
    public async Task Does_Not_Report_Trigger_Rules_When_Trigger_Is_Not_Constant() {
        const string body = """
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Trigger", trigger);
        builder.AddAttribute(2, "Shown", true);
        builder.AddAttribute(3, "Repeat", 1);
        builder.AddAttribute(4, "OnEntered", new object());
        builder.CloseComponent();
""";

        Assert.Empty(await GetDiagnosticsAsync(body));
    }

    [Fact]
    public async Task Ignores_Other_Components_And_Nested_Animations() {
        const string body = """
        builder.OpenComponent<global::NTComponents.NTAnimation>(0);
        builder.AddAttribute(1, "Trigger", global::NTComponents.NTAnimationTrigger.Manual);
        builder.OpenComponent<global::NTComponents.OtherComponent>(2);
        builder.AddAttribute(3, "Threshold", 5);
        builder.AddAttribute(4, "Repeat", 1);
        builder.CloseComponent();
        builder.AddAttribute(5, "Shown", true);
        builder.CloseComponent();
""";

        Assert.Empty(await GetDiagnosticsAsync(body));
    }

    [Fact]
    public async Task Does_Not_Report_When_Animation_Type_Is_Missing() {
        Assert.Empty(await GetCompilationDiagnosticsAsync("public static class Factory { public static void Build() { } }"));
    }

    private static Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string body) {
        var source = $$"""
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Rendering;

public static class AnimationFactory {
    public static void Build(RenderTreeBuilder builder, global::NTComponents.NTAnimationTrigger trigger) {
{{body}}
    }
}

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
    public class NTAnimation { }
    public class OtherComponent { }
    public enum NTAnimationTrigger { Visible, Render, Manual }
}
""";
        return GetCompilationDiagnosticsAsync(source);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetCompilationDiagnosticsAsync(string source) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), "AnimationFactory.cs");
        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create("AnalyzerTests", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return await compilation
            .WithAnalyzers([new NTAnimationConfigurationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}
