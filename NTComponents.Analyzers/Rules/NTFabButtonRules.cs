using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTFabButton. Each one mirrors a check NTFabButton.OnParametersSet throws for or warns about.</summary>
internal static class NTFabButtonRules {

    public static readonly DiagnosticDescriptor MissingIcon = new(
        "NTC1032",
        "NTFabButton icon is required",
        "NTFabButton requires a non-null Icon parameter",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1032");

    public static readonly DiagnosticDescriptor MissingIconOnlyAriaLabel = new(
        "NTC1033",
        "Icon-only NTFabButton requires an aria label",
        "Icon-only NTFabButton requires a non-empty AriaLabel",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1033");

    public static readonly DiagnosticDescriptor InvalidLabel = new(
        "NTC1034",
        "NTFabButton label cannot contain line breaks",
        "NTFabButton Label must not contain line breaks",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1034");

    public static readonly DiagnosticDescriptor InvisibleBackground = new(
        "NTC1035",
        "NTFabButton background must be visible",
        "NTFabButton BackgroundColor must be a visible container color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1035");

    public static readonly DiagnosticDescriptor InvisibleTextColor = new(
        "NTC1036",
        "NTFabButton text color must be visible",
        "NTFabButton TextColor must be a visible content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1036");

    public static readonly DiagnosticDescriptor UnsupportedSize = new(
        "NTC1037",
        "NTFabButton size is unsupported",
        "NTFabButton does not support ButtonSize '{0}' and will render with '{1}'",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1037");

    public static readonly DiagnosticDescriptor InvalidPlacement = new(
        "NTC1038",
        "NTFabButton placement is invalid",
        "NTFabButton Placement must be Inline, LowerRight, LowerLeft, UpperRight, or UpperLeft",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1038");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingIcon, MissingIconOnlyAriaLabel, InvalidLabel, InvisibleBackground, InvisibleTextColor, UnsupportedSize, InvalidPlacement);
        registry.On("NTComponents.NTFabButton", Analyze);
    }

    /// <summary>The size NTFabButton and NTFabMenu render for an NTSize they do not support, or null when the size is supported or unknown.</summary>
    internal static string? FallbackSize(string? size) => size switch {
        "Smallest" => "Small",
        "Largest" => "Large",
        _ => null
    };

    /// <summary>True when Placement is a constant that names no NTFabButtonPlacement member, such as a cast of 99.</summary>
    internal static bool IsUndefinedPlacement(ComponentFrame frame, out Location location) {
        location = frame.LocationOf("Placement");
        return frame.Attributes.TryGetValue("Placement", out var placement) && OperationConstants.TryGetEnum(placement.Value, out var value) && value.Member is null;
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (NTIconButtonRules.IsOmittedOrNull(frame, "Icon")) {
            context.Report(MissingIcon, frame.LocationOf("Icon"));
        }

        // Icon-only means the Label is provably blank; an unknown Label might be supplied at runtime.
        if (frame.GetStringState("Label") == StringState.Empty && frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(MissingIconOnlyAriaLabel, frame.LocationOf("AriaLabel"));
        }

        if (frame.Attributes.TryGetValue("Label", out var label) && OperationConstants.TryGetString(label.Value, out var text) && text is not null && (text.Contains('\r') || text.Contains('\n'))) {
            context.Report(InvalidLabel, label.Location);
        }

        if (frame.GetEnumName("BackgroundColor") == "Transparent") {
            context.Report(InvisibleBackground, frame.LocationOf("BackgroundColor"));
        }

        if (frame.GetEnumName("TextColor") == "Transparent") {
            context.Report(InvisibleTextColor, frame.LocationOf("TextColor"));
        }

        var size = frame.GetEnumName("ButtonSize");
        if (FallbackSize(size) is { } fallback) {
            context.Report(UnsupportedSize, frame.LocationOf("ButtonSize"), size, fallback);
        }

        if (IsUndefinedPlacement(frame, out var location)) {
            context.Report(InvalidPlacement, location);
        }
    }
}
