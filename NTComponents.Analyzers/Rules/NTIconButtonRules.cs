using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTIconButton. Each one mirrors a check NTIconButton.OnParametersSet throws for.</summary>
internal static class NTIconButtonRules {

    public static readonly DiagnosticDescriptor MissingIcon = new(
        "NTC1026",
        "NTIconButton icon is required",
        "NTIconButton requires a non-null Icon parameter",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1026");

    public static readonly DiagnosticDescriptor EmptyAriaLabel = new(
        "NTC1027",
        "NTIconButton aria label cannot be empty",
        "NTIconButton requires a non-empty AriaLabel",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1027");

    public static readonly DiagnosticDescriptor OpaqueBackground = new(
        "NTC1028",
        "NTIconButton background must be transparent for this variant",
        "NTIconButton variant '{0}' must use a transparent BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1028");

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1029",
        "NTIconButton background must be visible for this variant",
        "NTIconButton variant '{0}' must use a visible container BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1029");

    public static readonly DiagnosticDescriptor InvisibleTextColor = new(
        "NTC1030",
        "NTIconButton text color must be visible",
        "NTIconButton TextColor must be a visible icon color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1030");

    public static readonly DiagnosticDescriptor InvalidElevation = new(
        "NTC1031",
        "NTIconButton elevation is invalid for this variant",
        "NTIconButton variant '{0}' cannot use Elevation '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1031");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingIcon, EmptyAriaLabel, OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation);
        registry.On("NTComponents.NTIconButton", Analyze);
    }

    /// <summary>True when a required, non-nullable parameter such as Icon is omitted or explicitly null. Any other value counts as present.</summary>
    internal static bool IsOmittedOrNull(ComponentFrame frame, string name) =>
        frame.Attributes.TryGetValue(name, out var attribute) ? OperationConstants.IsNull(attribute.Value) : !frame.HasSplattedAttributes;

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (IsOmittedOrNull(frame, "Icon")) {
            context.Report(MissingIcon, frame.LocationOf("Icon"));
        }

        if (frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(EmptyAriaLabel, frame.LocationOf("AriaLabel"));
        }

        // NTIconButton defaults to the Text variant (the Material 3 standard icon button), unlike NTButton.
        ButtonVariantChecks.Analyze(context, frame.GetEnumName("Variant", "Text"), ButtonVariantChecks.IsSelectedToggle(frame), OpaqueBackground, TransparentBackground, InvisibleTextColor, InvalidElevation);
    }
}
