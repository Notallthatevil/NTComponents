using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Accessible-name rules for NT form fields and sections.</summary>
internal static class NTInputAccessibilityRules {

    /// <summary>Message args: the component, and how to name it.</summary>
    public static readonly DiagnosticDescriptor MissingAccessibleName = new(
        "NTC1107",
        "Form field has no accessible name",
        "{0} has no accessible name; set {1}",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1107");

    public static readonly DiagnosticDescriptor FieldsetWithoutLegend = new(
        "NTC1108",
        "NTFormSectionView fieldset needs a heading",
        "NTFormSectionView with UseFieldset renders a fieldset without a legend when Heading is empty",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1108");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(MissingAccessibleName, FieldsetWithoutLegend);
        registry.Uses("NTComponents.NTRichTextEditor");
        // These render a native control named by Label. NTInputRangeSlider is left out: its handles fall back to "Minimum" and
        // "Maximum", and NTRichTextEditor falls back to Placeholder or the field name.
        registry.On("NTComponents.NTFieldBase`1", AnalyzeField);
        registry.On("NTComponents.NTBooleanInputBase", AnalyzeField);
        registry.On("NTComponents.NTInputSlider`1", AnalyzeField);
        registry.On("NTComponents.NTColorPicker", AnalyzeColorPicker);
        registry.On("NTComponents.NTFormSectionView", AnalyzeSection);
    }

    private static void AnalyzeField(ComponentContext context) {
        var frame = context.Frame;
        if (frame.HasSplattedAttributes || frame.IsOrDerivesFrom("NTComponents.NTRichTextEditor")) {
            return;
        }

        if (frame.GetStringState("Label") == StringState.Empty
            && frame.GetStringState("aria-label") == StringState.Empty
            && frame.GetStringState("aria-labelledby") == StringState.Empty) {
            context.Report(MissingAccessibleName, NTInputRules.Where(frame, "Label"), frame.Type.Name, "Label, aria-label, or aria-labelledby");
        }
    }

    private static void AnalyzeColorPicker(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Has("AriaLabel") && frame.GetStringState("AriaLabel", "Color picker") == StringState.Empty) {
            context.Report(MissingAccessibleName, NTInputRules.Where(frame, "AriaLabel"), frame.Type.Name, "AriaLabel");
        }
    }

    private static void AnalyzeSection(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetBool("UseFieldset") == true && frame.GetStringState("Heading") == StringState.Empty) {
            context.Report(FieldsetWithoutLegend, NTInputRules.Where(frame, "Heading"));
        }
    }
}
