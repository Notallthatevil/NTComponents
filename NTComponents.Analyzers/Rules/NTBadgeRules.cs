using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for badges: NTBadge itself and the NT components that implement INTBadgeable.</summary>
internal static class NTBadgeRules {
    // The components share no base class that implements INTBadgeable, so each is registered by name.
    private static readonly string[] _badgeableComponents = [
        "NTComponents.NTButton",
        "NTComponents.NTIconButton",
        "NTComponents.NTFabButton",
        "NTComponents.NTChip",
        "NTComponents.NTTab",
        "NTComponents.NTNavigationRailItem",
        "NTComponents.NTMenuAnchorItem",
        "NTComponents.NTMenuButtonItem",
        "NTComponents.NTMenuSubMenuItem"
    ];

    /// <summary>Message args: the component, and the parameter that would name the badge.</summary>
    public static readonly DiagnosticDescriptor DotBadgeWithoutLabel = new(
        "NTC1101",
        "Dot badge has no accessible label",
        "{0} shows a dot badge without an accessible label; set {1}",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1101");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(DotBadgeWithoutLabel);
        foreach (var component in _badgeableComponents) {
            registry.On(component, AnalyzeHost);
        }

        registry.On("NTComponents.NTBadge", AnalyzeBadge);
    }

    private static void AnalyzeHost(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetBool("ShowBadge") == true
            && frame.GetStringState("BadgeContent") == StringState.Empty
            && frame.GetStringState("BadgeAriaLabel") == StringState.Empty) {
            context.Report(DotBadgeWithoutLabel, frame.LocationOf("ShowBadge"), frame.Type.Name, "BadgeAriaLabel");
        }
    }

    /// <summary>A badge without Content renders only the small dot, so the only text assistive technology can read is AriaLabel.</summary>
    private static void AnalyzeBadge(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Content") == StringState.Empty && frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(DotBadgeWithoutLabel, frame.Location, "NTBadge", "AriaLabel");
        }
    }
}
