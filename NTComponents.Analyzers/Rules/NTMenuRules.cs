using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTMenu, NTContextMenu, and menu items. Also holds the actionable-item check NTSplitButton shares.</summary>
internal static class NTMenuRules {
    private static readonly string[] ActionableItems = ["NTComponents.NTMenuButtonItem", "NTComponents.NTMenuAnchorItem", "NTComponents.NTMenuSubMenuItem"];
    private static readonly string[] PassiveItems = ["NTComponents.NTMenuDividerItem", "NTComponents.NTMenuLabelItem"];
    private static readonly string[] ColorParameters = ["ContainerColor", "TextColor", "SelectedContainerColor", "SelectedTextColor"];

    public static readonly DiagnosticDescriptor MissingAriaLabel = new(
        "NTC1047",
        "NTMenu requires an aria label",
        "NTMenu requires a non-empty AriaLabel that describes the menu",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1047");

    public static readonly DiagnosticDescriptor MissingActionableItem = new(
        "NTC1048",
        "NTMenu needs an actionable item",
        "NTMenu requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1048");

    public static readonly DiagnosticDescriptor EmptyItemLabel = new(
        "NTC1049",
        "NTMenu item label cannot be empty",
        "{0} requires a non-empty Label",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1049");

    public static readonly DiagnosticDescriptor EmptyItemHref = new(
        "NTC1050",
        "NTMenu anchor item href cannot be empty",
        "NTMenuAnchorItem requires a non-empty Href",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1050");

    public static readonly DiagnosticDescriptor InvisibleColor = new(
        "NTC1051",
        "NTMenu colors must be visible",
        "NTMenu {0} must be a visible menu color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1051");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingAriaLabel, MissingActionableItem, EmptyItemLabel, EmptyItemHref, InvisibleColor);
        registry.Uses([.. ActionableItems, .. PassiveItems]);
        registry.On("NTComponents.NTMenu", context => AnalyzeMenu(context, "ChildContent", checkItems: true));
        // Inherited parent menu content can supply the actionable items, so only a context menu known not to inherit is checked.
        registry.On("NTComponents.NTContextMenu", context => AnalyzeMenu(context, "MenuContent", context.Frame.GetBool("InheritParentMenus") == false));
        registry.On("NTComponents.NTMenuButtonItem", AnalyzeItemLabel);
        registry.On("NTComponents.NTMenuSubMenuItem", AnalyzeItemLabel);
        registry.On("NTComponents.NTMenuLabelItem", AnalyzeItemLabel);
        registry.On("NTComponents.NTMenuAnchorItem", context => {
            AnalyzeItemLabel(context);
            if (context.Frame.GetStringState("Href") == StringState.Empty) {
                context.Report(EmptyItemHref, context.Frame.LocationOf("Href"));
            }
        });
    }

    /// <summary>
    ///     Reports <paramref name="rule" /> when the menu content named <paramref name="contentName" /> is missing, or fully visible and
    ///     holds no button, anchor or submenu item. Content the walker cannot fully see (an opaque fragment, or a component other than an
    ///     NT menu item that may render items) is never reported. A conditional item still counts as present.
    /// </summary>
    public static void RequireActionableItem(ComponentContext context, string contentName, DiagnosticDescriptor rule) {
        var frame = context.Frame;
        if (frame.HasSplattedAttributes) {
            return;
        }

        if (frame.Has(contentName)) {
            var content = frame.Descendants().Where(descendant => IsInside(frame.Attributes[contentName], descendant)).ToList();
            if (content.Any(static item => IsAny(item, ActionableItems))
                || frame.RendersOpaqueContent
                || content.Any(static item => item.RendersOpaqueContent || (!IsAny(item, ActionableItems) && !IsAny(item, PassiveItems)))) {
                return;
            }
        }

        context.Report(rule, frame.Location);
    }

    private static void AnalyzeMenu(ComponentContext context, string contentName, bool checkItems) {
        var frame = context.Frame;
        if (frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(MissingAriaLabel, frame.LocationOf("AriaLabel"));
        }

        foreach (var name in ColorParameters) {
            if (frame.GetEnumName(name) == "Transparent") {
                context.Report(InvisibleColor, frame.LocationOf(name), name);
            }
        }

        if (checkItems) {
            RequireActionableItem(context, contentName, MissingActionableItem);
        }
    }

    private static void AnalyzeItemLabel(ComponentContext context) {
        if (context.Frame.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyItemLabel, context.Frame.LocationOf("Label"), context.Frame.Type.Name);
        }
    }

    private static bool IsAny(ComponentFrame frame, string[] metadataNames) => metadataNames.Any(frame.IsOrDerivesFrom);

    /// <summary>True when <paramref name="frame" /> is opened inside the value of <paramref name="content" />, so a sibling fragment such as TargetContent is ignored.</summary>
    private static bool IsInside(ComponentAttribute content, ComponentFrame frame) =>
        OperationConstants.Unwrap(content.Value)?.Syntax is not { } syntax || (syntax.SyntaxTree == frame.OpenSyntax.SyntaxTree && syntax.Span.Contains(frame.OpenSyntax.Span));
}
