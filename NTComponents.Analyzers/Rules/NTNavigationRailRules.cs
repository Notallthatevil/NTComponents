using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTNavigationRail and the items, groups and section headers rendered inside it.</summary>
internal static class NTNavigationRailRules {
    private static readonly string[] _defaultMenuButtonParameters = ["MenuIcon", "MenuIconWhenExpanded", "MenuAriaLabel", "MenuAriaLabelWhenExpanded"];
    private static readonly string[] _buttonOnlyItemParameters = ["Type", "ElementName", "Value"];

    public static readonly DiagnosticDescriptor EmptyAriaLabel = new(
        "NTC1092",
        "NTNavigationRail AriaLabel cannot be empty",
        "NTNavigationRail requires a non-empty AriaLabel because it provides the navigation landmark name",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1092");

    public static readonly DiagnosticDescriptor EmptyLabel = new(
        "NTC1093",
        "Navigation rail entry label cannot be empty",
        "{0} requires a non-empty Label",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1093");

    public static readonly DiagnosticDescriptor InvalidAriaLevel = new(
        "NTC1094",
        "NTNavigationRailSectionHeader AriaLevel is out of range",
        "NTNavigationRailSectionHeader AriaLevel must be between 1 and 6",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1094");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyAriaLabel, EmptyLabel, InvalidAriaLevel);
        registry.On("NTComponents.NTNavigationRail", AnalyzeRail);
        registry.On("NTComponents.NTNavigationRailItem", AnalyzeLabel);
        registry.On("NTComponents.NTNavigationRailItem", AnalyzeItem);
        registry.On("NTComponents.NTNavigationRailGroup", AnalyzeLabel);
        registry.On("NTComponents.NTNavigationRailSectionHeader", AnalyzeLabel);
        registry.On("NTComponents.NTNavigationRailSectionHeader", AnalyzeSectionHeader);
    }

    private static void AnalyzeRail(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(EmptyAriaLabel, frame.LocationOf("AriaLabel"));
        }

        if (RenderFragmentChecks.IsSet(frame, "Menu") != true) {
            return;
        }

        // A custom Menu replaces the default menu button, so only its href is unused; the icons and labels still feed the
        // external (small screen) menu button unless that button is not rendered either.
        if (frame.Has("MenuHref")) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("MenuHref"), "NTNavigationRail MenuHref", "when Menu is set");
        }

        if (frame.GetBool("HideRailOnXSScreens", true) != false || frame.GetEnumName("CollapseBehavior", "Rail") != "Rail") {
            return;
        }

        foreach (var name in _defaultMenuButtonParameters) {
            if (frame.Has(name)) {
                context.Report(CommonRules.NoEffect, frame.LocationOf(name), "NTNavigationRail " + name, "when Menu is set and HideRailOnXSScreens is false");
            }
        }
    }

    private static void AnalyzeLabel(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyLabel, frame.LocationOf("Label"), frame.Type.Name);
        }
    }

    private static void AnalyzeItem(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Href") != StringState.NonEmpty) {
            return;
        }

        foreach (var name in _buttonOnlyItemParameters) {
            if (frame.Has(name)) {
                context.Report(CommonRules.NoEffect, frame.LocationOf(name), "NTNavigationRailItem " + name, "when Href is set because the item renders a link");
            }
        }
    }

    private static void AnalyzeSectionHeader(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetNumber("AriaLevel") is < 1 or > 6) {
            context.Report(InvalidAriaLevel, frame.LocationOf("AriaLevel"));
        }
    }
}
