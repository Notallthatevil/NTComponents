using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NTComponents.Analyzers;

namespace NTComponents.Analyzers.Tests;

internal sealed class ColorMigrationDiagnosticProvider : FixAllContext.DiagnosticProvider {
    public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) {
        var tree = await document.GetSyntaxTreeAsync(cancellationToken);
        var diagnostics = await GetAllDiagnosticsAsync(document.Project, cancellationToken);
        return diagnostics.Where(diagnostic => diagnostic.Location.SourceTree == tree);
    }

    public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) => Task.FromResult(Enumerable.Empty<Diagnostic>());

    public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) {
        var compilation = await project.GetCompilationAsync(cancellationToken);
        return await compilation!.WithAnalyzers([new TnTColorMigrationAnalyzer()]).GetAnalyzerDiagnosticsAsync(cancellationToken);
    }
}
