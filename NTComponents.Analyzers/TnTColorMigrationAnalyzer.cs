using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NTComponents.Analyzers;

/// <summary>Identifies legacy color references that need migration to NTColor.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TnTColorMigrationAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "NTC1077";
    public const string RemovedColorDiagnosticId = "NTC1078";
    public const string CssHelperDiagnosticId = "NTC1079";

    private static readonly DiagnosticDescriptor MigrationRule = new(DiagnosticId, "Use NTColor", "Use NTColor instead of TnTColor", "Migration", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RemovedColorRule = new(RemovedColorDiagnosticId, "Choose a supported NT color", "TnTColor.{0} has no NTColor equivalent; use an optional color or choose a semantic color role", "Migration", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor CssHelperRule = new(CssHelperDiagnosticId, "Use NT theme color variables", "Use ToCssNTColorVariable instead of ToCssTnTColorVariable", "Migration", DiagnosticSeverity.Warning, true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MigrationRule, RemovedColorRule, CssHelperRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            var legacyType = start.Compilation.GetTypeByMetadataName("NTComponents.TnTColor");
            var newType = start.Compilation.GetTypeByMetadataName("NTComponents.NTColor");
            if (legacyType is null || newType is null) {
                return;
            }

            start.RegisterSyntaxNodeAction(nodeContext => {
                var name = (IdentifierNameSyntax)nodeContext.Node;
                var symbolInfo = nodeContext.SemanticModel.GetSymbolInfo(name, nodeContext.CancellationToken);
                var symbol = symbolInfo.Symbol;
                if (symbol is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.ContainingType, legacyType)
                    && newType.GetMembers(field.Name).Length == 0) {
                    nodeContext.ReportDiagnostic(Diagnostic.Create(RemovedColorRule, name.GetLocation(), field.Name));
                    return;
                }

                var method = symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                var receiverType = name.Parent is MemberAccessExpressionSyntax helperAccess
                    ? nodeContext.SemanticModel.GetTypeInfo(helperAccess.Expression, nodeContext.CancellationToken).Type as INamedTypeSymbol
                    : null;
                if (receiverType?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
                    receiverType = receiverType.TypeArguments[0] as INamedTypeSymbol;
                }
                if (name.Identifier.ValueText == "ToCssTnTColorVariable"
                    && (method?.ContainingType.ToDisplayString() == "NTComponents.TnTColorEnumExt"
                        || SymbolEqualityComparer.Default.Equals(receiverType, newType))) {
                    nodeContext.ReportDiagnostic(Diagnostic.Create(CssHelperRule, name.GetLocation()));
                    return;
                }

                if (!SymbolEqualityComparer.Default.Equals(symbol, legacyType)
                    || nodeContext.SemanticModel.GetAliasInfo(name, nodeContext.CancellationToken) is not null) {
                    return;
                }

                SyntaxNode typeExpression = name.Parent is MemberAccessExpressionSyntax qualifiedType && qualifiedType.Name == name ? qualifiedType : name;
                if (typeExpression.Parent is MemberAccessExpressionSyntax access
                    && nodeContext.SemanticModel.GetSymbolInfo(access, nodeContext.CancellationToken).Symbol is IFieldSymbol member
                    && newType.GetMembers(member.Name).Length == 0) {
                    return;
                }

                nodeContext.ReportDiagnostic(Diagnostic.Create(MigrationRule, name.GetLocation()));
            }, SyntaxKind.IdentifierName);
        });
    }
}
