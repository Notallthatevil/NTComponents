using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTFabMenu and its button and anchor items.</summary>
internal static class NTFabMenuRules {
    private static readonly string[] ColorParameters = ["BackgroundColor", "TextColor", "SelectedFabBackgroundColor", "SelectedFabTextColor", "MenuItemBackgroundColor", "MenuItemTextColor"];

    public static readonly DiagnosticDescriptor MissingIcon = new(
        "NTC1039",
        "NTFabMenu icon is required",
        "NTFabMenu requires a non-null Icon parameter",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1039");

    public static readonly DiagnosticDescriptor MissingAriaLabel = new(
        "NTC1040",
        "NTFabMenu requires an aria label",
        "NTFabMenu requires a non-empty AriaLabel that describes the menu opened by the FAB",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1040");

    public static readonly DiagnosticDescriptor InvisibleColor = new(
        "NTC1041",
        "NTFabMenu colors must be visible",
        "NTFabMenu {0} must be a visible color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1041");

    public static readonly DiagnosticDescriptor UnsupportedSize = new(
        "NTC1042",
        "NTFabMenu size is unsupported",
        "NTFabMenu does not support ButtonSize '{0}' and will render with '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1042");

    public static readonly DiagnosticDescriptor InvalidPlacement = new(
        "NTC1043",
        "NTFabMenu placement is invalid",
        "NTFabMenu Placement must be Inline, LowerRight, LowerLeft, UpperRight, or UpperLeft",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1043");

    public static readonly DiagnosticDescriptor InvalidItemCount = new(
        "NTC1044",
        "NTFabMenu requires 2 to 6 menu items",
        "NTFabMenu requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1044");

    public static readonly DiagnosticDescriptor EmptyItemLabel = new(
        "NTC1045",
        "NTFabMenu item label cannot be empty",
        "{0} requires a non-empty Label",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1045");

    public static readonly DiagnosticDescriptor EmptyItemHref = new(
        "NTC1046",
        "NTFabMenu anchor item href cannot be empty",
        "NTFabMenuAnchorItem requires a non-empty Href",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1046");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingIcon, MissingAriaLabel, InvisibleColor, UnsupportedSize, InvalidPlacement, InvalidItemCount, EmptyItemLabel, EmptyItemHref);
        registry.On("NTComponents.NTFabMenu", AnalyzeMenu);
        registry.On("NTComponents.NTFabMenuButtonItem", AnalyzeItem);
        registry.On("NTComponents.NTFabMenuAnchorItem", AnalyzeItem);
        registry.On("NTComponents.NTFabMenuAnchorItem", AnalyzeAnchorItem);
    }

    private static void AnalyzeMenu(ComponentContext context) {
        var menu = context.Frame;
        if (NTIconButtonRules.IsOmittedOrNull(menu, "Icon")) {
            context.Report(MissingIcon, menu.LocationOf("Icon"));
        }

        if (menu.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(MissingAriaLabel, menu.LocationOf("AriaLabel"));
        }

        foreach (var name in ColorParameters) {
            if (menu.GetEnumName(name) == "Transparent") {
                context.Report(InvisibleColor, menu.LocationOf(name), name);
            }
        }

        var size = menu.GetEnumName("ButtonSize");
        if (NTFabButtonRules.FallbackSize(size) is { } fallback) {
            context.Report(UnsupportedSize, menu.LocationOf("ButtonSize"), size, fallback);
        }

        if (NTFabButtonRules.IsUndefinedPlacement(menu, out var location)) {
            context.Report(InvalidPlacement, location);
        }

        if (HasInvalidItemCount(menu)) {
            context.Report(InvalidItemCount, menu.Location);
        }
    }

    /// <summary>
    ///     NTFabMenu counts the items that register with it on first render, wherever they sit in its content. Items under an @if
    ///     or loop, or content the walker cannot see into (@ChildContent, components that may render items), make the count
    ///     uncertain, so only a count that is already too high is reported then.
    /// </summary>
    private static bool HasInvalidItemCount(ComponentFrame menu) {
        if (menu.HasSplattedAttributes) {
            return false;
        }

        if (!menu.Has("ChildContent")) {
            return true;
        }

        var descendants = menu.Descendants().ToList();
        var items = descendants.Where(IsItem).ToList();
        var certain = items.Where(item => !IsConditionalWithin(item, menu)).ToList();
        var isUncertain = menu.RendersOpaqueContent
            || certain.Count != items.Count
            || descendants.Any(descendant => !IsItem(descendant) && (!descendant.IsNTComponent || descendant.RendersOpaqueContent));
        return isUncertain ? certain.Count > 6 : items.Count is < 2 or > 6;
    }

    private static void AnalyzeItem(ComponentContext context) {
        var item = context.Frame;
        if (item.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyItemLabel, item.LocationOf("Label"), item.Type.Name);
        }
    }

    private static void AnalyzeAnchorItem(ComponentContext context) {
        var item = context.Frame;
        if (item.GetStringState("Href") == StringState.Empty) {
            context.Report(EmptyItemHref, item.LocationOf("Href"));
        }
    }

    private static bool IsItem(ComponentFrame frame) => frame.MetadataName is "NTComponents.NTFabMenuButtonItem" or "NTComponents.NTFabMenuAnchorItem";

    /// <summary>True when an @if or loop sits between the item and the menu.</summary>
    private static bool IsConditionalWithin(ComponentFrame item, ComponentFrame menu) =>
        item.IsConditional || item.Ancestors().TakeWhile(ancestor => ancestor != menu).Any(static ancestor => ancestor.IsConditional);
}
