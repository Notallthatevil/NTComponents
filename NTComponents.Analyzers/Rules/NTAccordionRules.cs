using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTAccordion and NTAccordionItem.</summary>
internal static class NTAccordionRules {

    public static readonly DiagnosticDescriptor MissingHeader = new(
        "NTC1096",
        "NTAccordionItem requires a Label or HeaderContent",
        "NTAccordionItem requires either a non-empty Label or HeaderContent",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1096");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingHeader);
        registry.On("NTComponents.NTAccordion", AnalyzeAccordion);
        registry.On("NTComponents.NTAccordionItem", AnalyzeItem);
    }

    private static void AnalyzeAccordion(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("GroupName") == StringState.NonEmpty && frame.GetBool("LimitToOneExpanded") == false) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("GroupName"), "NTAccordion GroupName", "unless LimitToOneExpanded is true");
        }
    }

    private static void AnalyzeItem(ComponentContext context) {
        var frame = context.Frame;
        var label = frame.GetStringState("Label");
        var headerContent = frame.IsFragmentSet("HeaderContent");
        if (label == StringState.Empty && headerContent == false) {
            context.Report(MissingHeader, frame.LocationOf("Label"));
        }
        else if (label == StringState.NonEmpty && headerContent == true) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("Label"), "NTAccordionItem Label", "when HeaderContent is set");
        }
    }
}
