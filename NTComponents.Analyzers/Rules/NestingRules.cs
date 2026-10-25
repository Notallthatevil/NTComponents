using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for components that read a cascading parent and throw without it, and for NTForm, which throws when nested.</summary>
internal static class NestingRules {

    public static readonly DiagnosticDescriptor MissingParent = new(
        "NTC1087",
        "Component must be rendered inside its required parent",
        "{0} must be rendered inside {1}",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1087");

    public static readonly DiagnosticDescriptor NestedForm = new(
        "NTC1088",
        "NTForm cannot be nested inside another NTForm",
        "NTForm cannot be nested inside another NTForm or an NTWizardFormStep",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1088");

    private static readonly string[] MenuHosts = ["NTComponents.NTMenu", "NTComponents.NTContextMenu", "NTComponents.NTSplitButton", "NTComponents.NTMenuSubMenuItem", "NTComponents.NTChip"];

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingParent, NestedForm);
        RequireParent(registry, "NTComponents.NTTab", "NTTabView", sameTypeArgument: false, "NTComponents.NTTabView");
        RequireParent(registry, "NTComponents.NTInputRadio`1", "an NTInputRadioGroup with the same TValue", sameTypeArgument: true, "NTComponents.NTInputRadioGroup`1");
        RequireParent(registry, "NTComponents.NTButtonGroupItem`1", "an NTButtonGroup with the same TObjectType", sameTypeArgument: true, "NTComponents.NTButtonGroup`1");
        RequireParent(registry, "NTComponents.NTDataGridColumn`1", "an NTDataGrid with the same TItem", sameTypeArgument: true, "NTComponents.NTDataGrid`1");
        RequireParent(registry, "NTComponents.NTFabMenuButtonItem", "NTFabMenu", sameTypeArgument: false, "NTComponents.NTFabMenu");
        RequireParent(registry, "NTComponents.NTFabMenuAnchorItem", "NTFabMenu", sameTypeArgument: false, "NTComponents.NTFabMenu");
        RequireParent(registry, "NTComponents.NTCarouselItem", "NTCarousel", sameTypeArgument: false, "NTComponents.NTCarousel");
        RequireParent(registry, "NTComponents.Wizard.NTWizardStepBase", "NTWizard", sameTypeArgument: false, "NTComponents.NTWizard");
        RequireParent(registry, "NTComponents.NTAutocompleteOption", "NTAutocomplete", sameTypeArgument: false, "NTComponents.NTAutocomplete");
        foreach (var item in new[] { "NTMenuButtonItem", "NTMenuAnchorItem", "NTMenuDividerItem", "NTMenuLabelItem", "NTMenuSubMenuItem" }) {
            RequireParent(registry, "NTComponents." + item, "NTMenu, NTContextMenu, NTSplitButton, NTMenuSubMenuItem, or NTChip menu content", sameTypeArgument: false, MenuHosts);
        }

        registry.Uses("NTComponents.NTWizardFormStep");
        registry.On("NTComponents.NTForm", static context => {
            if (context.Frame.Ancestors().Any(static ancestor => ancestor.IsOrDerivesFrom("NTComponents.NTForm") || ancestor.IsOrDerivesFrom("NTComponents.NTWizardFormStep"))) {
                context.Report(NestedForm, context.Frame.Location);
            }
        });
    }

    /// <summary>
    ///     Reports <paramref name="child" /> when no ancestor supplies a parent. A component's own markup can be rendered inside a
    ///     parent elsewhere, so only routable pages, whose whole tree is visible, are checked; a component from outside NTComponents
    ///     on the way up could supply the parent itself.
    /// </summary>
    private static void RequireParent(ComponentRuleRegistry registry, string child, string parentDescription, bool sameTypeArgument, params string[] parents) {
        registry.Uses(parents);
        registry.On(child, context => {
            var frame = context.Frame;
            var ancestors = frame.Ancestors().ToArray();
            if (!ancestors.Any(ancestor => parents.Any(ancestor.IsOrDerivesFrom) && (!sameTypeArgument || SameFirstTypeArgument(ancestor, frame)))
                && (ancestors.LastOrDefault() ?? frame).IsPageRoot
                && ancestors.All(static ancestor => ancestor.IsNTComponent)) {
                context.Report(MissingParent, frame.Location, frame.Type.Name, parentDescription);
            }
        });
    }

    private static bool SameFirstTypeArgument(ComponentFrame parent, ComponentFrame child) =>
        FirstTypeArgument(parent.Type) is { } parentArgument && FirstTypeArgument(child.Type) is { } childArgument && SymbolEqualityComparer.Default.Equals(parentArgument, childArgument);

    /// <summary>The first type argument of the nearest generic type in the base chain, such as TItem for NTPropertyColumn&lt;TItem, TValue&gt;.</summary>
    private static ITypeSymbol? FirstTypeArgument(INamedTypeSymbol type) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.TypeArguments.Length > 0) {
                return current.TypeArguments[0];
            }
        }

        return null;
    }
}
