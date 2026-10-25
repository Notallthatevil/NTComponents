using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NTComponents.Analyzers;

/// <summary>Passed to a rule when a component frame closes, after all of its parameters and children are known.</summary>
internal readonly struct ComponentContext(ComponentFrame frame, SyntaxNodeAnalysisContext analysis) {
    public ComponentFrame Frame { get; } = frame;

    public SemanticModel SemanticModel => analysis.SemanticModel;

    public CancellationToken CancellationToken => analysis.CancellationToken;

    public void Report(DiagnosticDescriptor rule, Location location, params object?[] messageArgs) => analysis.ReportDiagnostic(Diagnostic.Create(rule, location, messageArgs));
}
