using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTAnimation parameters that only apply to some triggers. Trigger defaults to Visible.</summary>
internal static class NTAnimationRules {
    private static readonly string[] VisibleOnlyParameters = ["Repeat", "Threshold", "RootMargin", "OnEntered", "OnExited"];

    public static readonly DiagnosticDescriptor ShownRequiresManual = new(
        "NTC1080",
        "NTAnimation Shown requires Manual trigger",
        "NTAnimation Shown only applies when Trigger is Manual",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1080");

    public static readonly DiagnosticDescriptor VisibleOnlyParameter = new(
        "NTC1081",
        "NTAnimation parameter requires Visible trigger",
        "NTAnimation {0} only applies when Trigger is Visible",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1081");

    public static readonly DiagnosticDescriptor ThresholdOutOfRange = new(
        "NTC1082",
        "NTAnimation Threshold must be between 0 and 1",
        "NTAnimation Threshold must be between 0 and 1; out-of-range values are clamped at runtime",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1082");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(ShownRequiresManual, VisibleOnlyParameter, ThresholdOutOfRange);
        registry.On("NTComponents.NTAnimation", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetNumber("Threshold") is { } threshold && !(threshold >= 0 && threshold <= 1)) {
            context.Report(ThresholdOutOfRange, frame.LocationOf("Threshold"));
        }

        var trigger = frame.GetEnumName("Trigger", "Visible");
        if (trigger is null) {
            return;
        }

        if (trigger != "Manual" && frame.Has("Shown")) {
            context.Report(ShownRequiresManual, frame.LocationOf("Shown"));
        }

        if (trigger == "Visible") {
            return;
        }

        foreach (var name in VisibleOnlyParameters) {
            if (frame.Has(name)) {
                context.Report(VisibleOnlyParameter, frame.LocationOf(name), name);
            }
        }
    }
}
