using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTDialog.</summary>
internal static class NTDialogRules {

    public static readonly DiagnosticDescriptor MissingAccessibleName = new(
        "NTC1100",
        "NTDialog needs an accessible name",
        "NTDialog has no accessible name; set Title, TitleContent, or a non-empty aria-label",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1100");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingAccessibleName);
        registry.On("NTComponents.NTDialog", Analyze);
    }

    /// <summary>
    ///     The dialog names itself from its title heading through aria-labelledby, which it writes after <c>@attributes</c> and so
    ///     replaces any value passed in. Only an aria-label passed through can name a dialog that has no title.
    /// </summary>
    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.HasSplattedAttributes
            || frame.GetStringState("Title") != StringState.Empty
            || RenderFragmentChecks.IsSet(frame, "TitleContent") != false
            || frame.GetStringState("aria-label") != StringState.Empty) {
            return;
        }

        context.Report(MissingAccessibleName, frame.Location);
    }
}
