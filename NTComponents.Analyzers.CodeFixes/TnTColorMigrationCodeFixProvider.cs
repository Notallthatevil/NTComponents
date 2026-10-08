using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace NTComponents.Analyzers.CodeFixes;

/// <summary>Replaces legacy color types and CSS conversion helpers, with Fix All support.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TnTColorMigrationCodeFixProvider)), Shared]
public sealed class TnTColorMigrationCodeFixProvider : CodeFixProvider {
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ["NTC1077", "NTC1079"];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span, getInnermostNodeForTie: true).FirstAncestorOrSelf<IdentifierNameSyntax>();
        if (node is null) {
            return;
        }

        foreach (var diagnostic in context.Diagnostics) {
            if (!FixableDiagnosticIds.Contains(diagnostic.Id)) {
                continue;
            }
            var isColorType = diagnostic.Id == "NTC1077";
            context.RegisterCodeFix(CodeAction.Create(isColorType ? "Use NTColor" : "Use NT theme variables",
                cancellation => ReplaceAsync(context.Document, node, isColorType, cancellation),
                equivalenceKey: diagnostic.Id), diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, IdentifierNameSyntax node, bool isColorType, CancellationToken cancellation) {
        var root = await document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);
        if (root is null) {
            return document;
        }

        SyntaxNode target = node;
        SyntaxNode replacement;
        if (isColorType) {
            // Qualify the replacement so aliases, missing imports, and local NTColor types remain safe.
            if (node.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax
                || node.Parent is MemberAccessExpressionSyntax access && access.Name == node) {
                target = node.Parent;
            }
            replacement = SyntaxFactory.ParseName("global::NTComponents.NTColor").WithAdditionalAnnotations(Simplifier.Annotation);
        }
        else {
            replacement = SyntaxFactory.IdentifierName("ToCssNTColorVariable");
            var model = await document.GetSemanticModelAsync(cancellation).ConfigureAwait(false);
            if (node.Parent is MemberAccessExpressionSyntax helperAccess
                && model?.GetSymbolInfo(helperAccess.Expression, cancellation).Symbol is INamedTypeSymbol helperType
                && helperType.ToDisplayString() == "NTComponents.TnTColorEnumExt") {
                target = helperAccess;
                replacement = SyntaxFactory.ParseExpression("global::NTComponents.NTColorEnumExt.ToCssNTColorVariable").WithAdditionalAnnotations(Simplifier.Annotation);
            }
        }

        return document.WithSyntaxRoot(root.ReplaceNode(target, replacement.WithTriviaFrom(target)));
    }
}
