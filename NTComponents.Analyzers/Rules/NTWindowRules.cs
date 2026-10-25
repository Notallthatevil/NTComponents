using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTWindow. Undefined State and DockPosition values are covered by the undefined enum rules.</summary>
internal static class NTWindowRules {

    public static readonly DiagnosticDescriptor EmptyTitle = new(
        "NTC1095",
        "NTWindow title cannot be empty",
        "NTWindow requires a non-empty Title",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1095");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyTitle);
        registry.On("NTComponents.NTWindow", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Title") == StringState.Empty) {
            context.Report(EmptyTitle, frame.LocationOf("Title"));
        }
    }
}
