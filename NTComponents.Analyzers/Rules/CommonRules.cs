using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Shared Info rules for parameters a component ignores or adjusts instead of rejecting.</summary>
internal static class CommonRules {

    /// <summary>Message args: the parameter, such as "NTAccordion GroupName", and the condition, such as "unless LimitToOneExpanded is true".</summary>
    public static readonly DiagnosticDescriptor NoEffect = new(
        "NTC1102",
        "Parameter has no effect in this configuration",
        "{0} has no effect {1}",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1102");

    /// <summary>Message args: the parameter, the value as written, and the value the component uses.</summary>
    public static readonly DiagnosticDescriptor Adjusted = new(
        "NTC1103",
        "Parameter value is adjusted at runtime",
        "{0} value {1} is changed to {2} at runtime",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1103");

    public static void Register(ComponentRuleRegistry registry) => registry.Describe(NoEffect, Adjusted);
}
