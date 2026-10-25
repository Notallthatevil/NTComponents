using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NTComponents.Analyzers;

/// <summary>
///     Checks how NT components are configured in Razor markup and hand-written render trees. One walk per member feeds every
///     component rule set.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NTComponentUsageAnalyzer : DiagnosticAnalyzer {

    internal static readonly ComponentRuleRegistry DefaultRegistry = ComponentRuleRegistry.Create(
        CommonRules.Register,
        NestingRules.Register,
        UndefinedEnumRules.Register,
        OwnedAttributeRules.Register,
        NTButtonRules.Register,
        NTIconButtonRules.Register,
        NTFabButtonRules.Register,
        NTFabMenuRules.Register,
        NTSplitButtonRules.Register,
        NTButtonGroupRules.Register,
        NTMenuRules.Register,
        NTNavLinkRules.Register,
        NTCardRules.Register,
        NTCarouselRules.Register,
        NTDataGridRules.Register,
        NTProgressRules.Register,
        NTAnimationRules.Register,
        NTInputRules.Register,
        NTChipRules.Register,
        NTNavigationRailRules.Register,
        NTWindowRules.Register,
        NTAccordionRules.Register,
        NTVirtualizeRules.Register,
        NTResponsiveRules.Register,
        NTDialogRules.Register,
        NTBadgeRules.Register);

    private readonly ComponentRuleRegistry _registry;

    /// <summary>Creates the analyzer with every NT component rule set.</summary>
    public NTComponentUsageAnalyzer() : this(DefaultRegistry) {
    }

    /// <summary>Creates the analyzer with a custom registry, for tests.</summary>
    internal NTComponentUsageAnalyzer(ComponentRuleRegistry registry) => _registry = registry;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _registry.Descriptors;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext => {
            if (startContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder") is not { } builderType) {
                return;
            }

            startContext.RegisterSyntaxNodeAction(
                nodeContext => RenderTreeWalker.Walk(nodeContext, builderType, frame => _registry.Analyze(new ComponentContext(frame, nodeContext))),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.FieldDeclaration);
        });
    }
}
