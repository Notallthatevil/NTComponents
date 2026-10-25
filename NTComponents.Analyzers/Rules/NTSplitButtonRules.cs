using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>
///     Rules for NTSplitButton. Its variant, color and elevation checks match <see cref="ButtonVariantChecks" /> (it has no toggle
///     state), and menu item Label and Href are covered by the NTMenu rules.
/// </summary>
internal static class NTSplitButtonRules {
    private static readonly string[] MenuColorParameters = ["MenuBackgroundColor", "MenuTextColor", "MenuSelectedBackgroundColor", "MenuSelectedTextColor"];

    public static readonly DiagnosticDescriptor EmptyLabel = new(
        "NTC1009",
        "NTSplitButton label cannot be empty without an icon",
        "NTSplitButton requires a non-empty Label unless LeadingIcon is supplied",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1009");

    public static readonly DiagnosticDescriptor MissingActionAriaLabel = new(
        "NTC1010",
        "Icon-only NTSplitButton action needs an accessible label",
        "Icon-only NTSplitButton actions require a non-empty ActionAriaLabel",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1010");

    public static readonly DiagnosticDescriptor OpaqueBackground = new(
        "NTC1011",
        "NTSplitButton background must be transparent for this variant",
        "NTSplitButton variant '{0}' must use a transparent BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1011");

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1012",
        "NTSplitButton background must be visible for this variant",
        "NTSplitButton variant '{0}' must use a visible container BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1012");

    public static readonly DiagnosticDescriptor InvisibleTextColor = new(
        "NTC1013",
        "NTSplitButton text color must be visible",
        "NTSplitButton TextColor must be a visible content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1013");

    public static readonly DiagnosticDescriptor InvalidElevation = new(
        "NTC1014",
        "NTSplitButton elevation is invalid for this variant",
        "NTSplitButton variant '{0}' cannot use Elevation '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1014");

    public static readonly DiagnosticDescriptor InvisibleMenuColor = new(
        "NTC1015",
        "NTSplitButton menu colors must be visible",
        "NTSplitButton {0} must be a visible menu color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1015");

    public static readonly DiagnosticDescriptor MissingActionableItem = new(
        "NTC1016",
        "NTSplitButton menu needs an actionable item",
        "NTSplitButton requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1016");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyLabel, MissingActionAriaLabel, OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation, InvisibleMenuColor, MissingActionableItem);
        registry.On("NTComponents.NTSplitButton", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Label", string.Empty) == StringState.Empty) {
            // An omitted LeadingIcon is only known to be missing when @attributes cannot supply it.
            if (frame.IsOmittedOrNull("LeadingIcon")) {
                context.Report(EmptyLabel, frame.LocationOf("Label"));
            }
            else if (frame.GetStringState("ActionAriaLabel") == StringState.Empty) {
                context.Report(MissingActionAriaLabel, frame.LocationOf("ActionAriaLabel"));
            }
        }

        ButtonVariantChecks.Analyze(context, frame.GetEnumName("Variant", "Filled"), isSelectedToggle: false, OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation);

        context.ReportTransparent(InvisibleMenuColor, MenuColorParameters);

        NTMenuRules.RequireActionableItem(context, "ChildContent", MissingActionableItem);
    }
}
