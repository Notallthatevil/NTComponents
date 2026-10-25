using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTChip. Each one mirrors a check NTChip.OnParametersSet throws for.</summary>
internal static class NTChipRules {

    public static readonly DiagnosticDescriptor EmptyLabel = new(
        "NTC1090",
        "NTChip label cannot be empty",
        "NTChip requires a non-empty Label",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1090");

    public static readonly DiagnosticDescriptor IncompatibleModes = new(
        "NTC1091",
        "NTChip modes cannot be combined",
        "NTChip {0} cannot be combined with {1}",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1091");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyLabel, IncompatibleModes);
        registry.On("NTComponents.NTChip", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("Label") == StringState.Empty) {
            context.Report(EmptyLabel, frame.LocationOf("Label"));
        }

        var hasHref = frame.GetStringState("Href") == StringState.NonEmpty;
        var hasMenu = RenderFragmentChecks.IsSet(frame, "MenuContent");
        if (hasHref && IsSelectable(frame, hasMenu) == true) {
            context.Report(IncompatibleModes, frame.LocationOf("Href"), "Href", "selectable behavior (Selectable, or the Filter variant)");
        }

        if (hasMenu != true) {
            return;
        }

        if (hasHref) {
            context.Report(IncompatibleModes, RenderFragmentChecks.MappedLocationOf(frame, "MenuContent"), "MenuContent", "Href");
        }

        if (frame.GetBool("Selectable") == true) {
            context.Report(IncompatibleModes, frame.LocationOf("Selectable"), "MenuContent", "Selectable");
        }
    }

    /// <summary>Mirrors NTChip.EffectiveSelectable: never with a menu, otherwise Selectable, which defaults to the Filter variant.</summary>
    private static bool? IsSelectable(ComponentFrame frame, bool? hasMenu) {
        if (hasMenu != false) {
            return hasMenu is null ? null : false;
        }

        if (frame.Attributes.TryGetValue("Selectable", out var attribute) && !OperationConstants.IsNull(attribute.Value)) {
            return OperationConstants.TryGetBool(attribute.Value, out var selectable) ? selectable : null;
        }

        if (!frame.Has("Selectable") && frame.HasSplattedAttributes) {
            return null;
        }

        return frame.GetEnumName("Variant", "Assist") is { } variant ? variant == "Filter" : null;
    }
}
