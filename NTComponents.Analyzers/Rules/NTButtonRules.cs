using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTButton. Each one mirrors a check NTButton.OnParametersSet throws for.</summary>
internal static class NTButtonRules {

    public static readonly DiagnosticDescriptor OpaqueBackground = new(
        "NTC1003",
        "NTButton background must be transparent for this variant",
        "NTButton variant '{0}' must use a transparent BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1003");

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1004",
        "NTButton background must be visible for this variant",
        "NTButton variant '{0}' must use a visible container BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1004");

    public static readonly DiagnosticDescriptor InvisibleTextColor = new(
        "NTC1005",
        "NTButton text color must be visible",
        "NTButton TextColor must be a visible content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1005");

    public static readonly DiagnosticDescriptor InvalidElevation = new(
        "NTC1006",
        "NTButton elevation is invalid for this variant",
        "NTButton variant '{0}' cannot use Elevation '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1006");

    public static readonly DiagnosticDescriptor TextToggle = new(
        "NTC1007",
        "Text NTButton cannot be a toggle button",
        "NTButton variant 'Text' does not support toggle behavior",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1007");

    public static readonly DiagnosticDescriptor EmptyLabel = new(
        "NTC1008",
        "NTButton label cannot be empty",
        "NTButton requires a non-empty Label",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1008");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation, TextToggle, EmptyLabel);
        registry.On("NTComponents.NTButton", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyLabel, frame.LocationOf("Label"));
        }

        var variant = frame.GetEnumName("Variant", "Filled");
        if (variant == "Text" && frame.GetBool("IsToggleButton") == true) {
            context.Report(TextToggle, frame.LocationOf("IsToggleButton"));
        }

        ButtonVariantChecks.Analyze(context, variant, ButtonVariantChecks.IsSelectedToggle(frame), OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation);
    }
}
