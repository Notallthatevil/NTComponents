using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     Rules for NTNavLink. Each one mirrors a check NTNavLink.OnParametersSet throws for. The default Variant is Filled. Elevated,
///     Filled, Tonal, Outlined and Text use button chrome; DefaultAnchor and InlineText use text-link chrome and have no background
///     or elevation checks.
/// </summary>
internal static class NTNavLinkRules {
    private static readonly string[] ContentColorParameters = ["TextColor", "HoverTextColor", "VisitedTextColor", "ActiveTextColor"];

    public static readonly DiagnosticDescriptor EmptyLabel = new(
        "NTC1056",
        "NTNavLink label cannot be empty",
        "NTNavLink requires a non-empty Label so the anchor has an accessible name",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1056");

    public static readonly DiagnosticDescriptor ComponentOwnedAriaState = new(
        "NTC1057",
        "NTNavLink owns this ARIA state",
        "Do not set '{0}' on NTNavLink. Use NTNavLink parameters and route matching so the component renders this ARIA state correctly.",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1057");

    public static readonly DiagnosticDescriptor DisabledTabIndex = new(
        "NTC1058",
        "Disabled NTNavLink cannot be focusable",
        "Disabled NTNavLink renders tabindex='-1'. Remove the explicit tabindex or do not disable the link.",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1058");

    public static readonly DiagnosticDescriptor OpaqueBackground = new(
        "NTC1083",
        "NTNavLink background must be transparent for this variant",
        "NTNavLink variant '{0}' must use a transparent BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1083");

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1084",
        "NTNavLink background must be visible for this variant",
        "NTNavLink variant '{0}' must use a visible container BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1084");

    public static readonly DiagnosticDescriptor InvisibleContentColor = new(
        "NTC1085",
        "NTNavLink content color must be visible",
        "NTNavLink {0} must be a visible content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1085");

    public static readonly DiagnosticDescriptor InvalidElevation = new(
        "NTC1086",
        "NTNavLink elevation is invalid for this variant",
        "NTNavLink variant '{0}' cannot use Elevation '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1086");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyLabel, ComponentOwnedAriaState, DisabledTabIndex, OpaqueBackground, TransparentBackground, InvisibleContentColor, InvalidElevation);
        registry.On("NTComponents.NTNavLink", Analyze);
    }

    /// <summary>Unmatched attributes such as <c>aria-current</c> have no markup location of their own, so fall back to the component.</summary>
    private static Location LocationOf(ComponentFrame frame, ComponentAttribute attribute) => SourceLocations.IsMapped(attribute.Location) ? attribute.Location : frame.Location;

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyLabel, frame.LocationOf("Label"));
        }

        // The component filters these out case-insensitively.
        foreach (var attribute in frame.Attributes.Values) {
            if (attribute.Name.Equals("aria-current", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("aria-disabled", StringComparison.OrdinalIgnoreCase)) {
                context.Report(ComponentOwnedAriaState, LocationOf(frame, attribute), attribute.Name);
            }
            else if (attribute.Name.Equals("tabindex", StringComparison.OrdinalIgnoreCase) && frame.GetBool("Disabled") == true) {
                context.Report(DisabledTabIndex, LocationOf(frame, attribute));
            }
        }

        var variant = frame.GetEnumName("Variant", "Filled");
        var buttonChrome = variant is "Elevated" or "Filled" or "Tonal" or "Outlined" or "Text";
        var textLinkChrome = variant is "DefaultAnchor" or "InlineText";

        if (buttonChrome && frame.GetEnumName("BackgroundColor") is { } background) {
            var needsTransparent = variant is "Text" or "Outlined";
            if (needsTransparent && background != "Transparent") {
                context.Report(OpaqueBackground, frame.LocationOf("BackgroundColor"), variant);
            }
            else if (!needsTransparent && background == "Transparent") {
                context.Report(TransparentBackground, frame.LocationOf("BackgroundColor"), variant);
            }
        }

        foreach (var name in ContentColorParameters) {
            // VisitedTextColor is only applied (and validated) with text-link chrome.
            if ((name != "VisitedTextColor" || textLinkChrome) && frame.GetEnumName(name) == "Transparent") {
                context.Report(InvisibleContentColor, frame.LocationOf(name), name);
            }
        }

        if (buttonChrome && frame.GetEnumName("Elevation") is { } elevation && (variant == "Elevated") == (elevation == "None")) {
            context.Report(InvalidElevation, frame.LocationOf("Elevation"), variant, elevation);
        }
    }
}
