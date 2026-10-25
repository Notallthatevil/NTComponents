using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTResponsive.</summary>
internal static class NTResponsiveRules {

    public static readonly DiagnosticDescriptor NegativeCustomBreakpoint = new(
        "NTC1099",
        "NTResponsive CustomBreakpoint cannot be negative",
        "NTResponsive CustomBreakpoint must be zero or greater",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1099");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(NegativeCustomBreakpoint);
        registry.On("NTComponents.NTResponsive", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        var custom = frame.GetNumber("CustomBreakpoint");
        if (custom < 0) {
            context.Report(NegativeCustomBreakpoint, frame.LocationOf("CustomBreakpoint"));
        }

        // The Breakpoint value is still validated, but a custom breakpoint decides the media query.
        if (custom >= 0 && frame.Has("Breakpoint")) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("Breakpoint"), "NTResponsive Breakpoint", "when CustomBreakpoint is set");
        }
    }
}
