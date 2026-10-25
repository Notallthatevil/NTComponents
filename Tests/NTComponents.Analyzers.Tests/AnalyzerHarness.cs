using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NTComponents.Analyzers.Tests;

/// <summary>Runs <see cref="NTComponentUsageAnalyzer" /> over C# compiled against the real NTComponents and ASP.NET Core assemblies.</summary>
internal static class AnalyzerHarness {
    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(() => [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(static path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(path) != "NTComponents.Analyzers.Tests.dll")
            .Select(static path => MetadataReference.CreateFromFile(path))
    ]);

    /// <summary>Analyzes statements placed in <c>static void Build(RenderTreeBuilder builder)</c>, with NTComponents imported.</summary>
    public static Task<ImmutableArray<Diagnostic>> AnalyzeBuildAsync(string statements) => AnalyzeAsync($$"""
        using System;
        using System.Linq;
        using Microsoft.AspNetCore.Components;
        using Microsoft.AspNetCore.Components.Rendering;
        using NTComponents;

        public static class Fixture {
            public static void Build(RenderTreeBuilder builder) {
        {{statements}}
            }
        }
        """);

    /// <summary>Analyzes complete C# sources. Fails the test when a source does not compile or a rule throws.</summary>
    public static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(params string[] sources) => AnalyzeAsync(new NTComponentUsageAnalyzer(), sources);

    /// <summary>Returns every component frame the walker builds for <paramref name="source" />, in close order.</summary>
    public static async Task<List<ComponentFrame>> CaptureFramesAsync(string source) {
        var frames = new List<ComponentFrame>();
        // Roslyn skips analyzers that support no diagnostics, so the capturing registry describes one it never reports.
        await AnalyzeAsync(new NTComponentUsageAnalyzer(ComponentRuleRegistry.Create(registry => {
            registry.Describe(CommonRules.NoEffect);
            registry.OnAny(context => frames.Add(context.Frame));
        })), source);
        return frames;
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(DiagnosticAnalyzer analyzer, params string[] sources) {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, $"Source{index}.cs")),
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, "Test source does not compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));

        var exceptions = new List<Exception>();
        var options = new CompilationWithAnalyzersOptions(new AnalyzerOptions([]), (exception, _, _) => exceptions.Add(exception), concurrentAnalysis: false, logAnalyzerExecutionTime: false);
        var diagnostics = await compilation.WithAnalyzers([analyzer], options).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(exceptions);
        return [.. diagnostics.OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start).ThenBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)];
    }

    /// <summary>The IDs of <paramref name="diagnostics" /> in source order, for compact assertions.</summary>
    public static string[] Ids(this IEnumerable<Diagnostic> diagnostics) => [.. diagnostics.Select(static diagnostic => diagnostic.Id)];
}
