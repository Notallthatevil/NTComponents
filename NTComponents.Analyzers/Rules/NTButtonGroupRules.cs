using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>
///     Rules for NTButtonGroup and NTButtonGroupItem. Each one mirrors a check NTButtonGroup.OnParametersSet or
///     NTButtonGroupItem.SetParametersAsync throws for. A group is selectable unless SelectionMode is None.
/// </summary>
internal static class NTButtonGroupRules {

    public static readonly DiagnosticDescriptor TextSelectable = new(
        "NTC1019",
        "Text NTButtonGroup cannot be selectable",
        "NTButtonGroup variant 'Text' does not support selectable behavior",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1019");

    public static readonly DiagnosticDescriptor OpaqueBackground = new(
        "NTC1020",
        "NTButtonGroup background must be transparent for this variant",
        "NTButtonGroup variant '{0}' must use a transparent BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1020");

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1021",
        "NTButtonGroup background must be visible for this variant",
        "NTButtonGroup variant '{0}' must use a visible container BackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1021");

    public static readonly DiagnosticDescriptor InvisibleTextColor = new(
        "NTC1022",
        "NTButtonGroup text color must be visible",
        "NTButtonGroup TextColor must be a visible content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1022");

    public static readonly DiagnosticDescriptor TransparentSelectedBackground = new(
        "NTC1023",
        "NTButtonGroup selected background must be visible",
        "Selectable NTButtonGroup variant '{0}' must use a visible selected container SelectedBackgroundColor",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1023");

    public static readonly DiagnosticDescriptor InvisibleSelectedTextColor = new(
        "NTC1024",
        "NTButtonGroup selected text color must be visible",
        "Selectable NTButtonGroup SelectedTextColor must be a visible selected content color",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1024");

    public static readonly DiagnosticDescriptor MissingIconOnlyAriaLabel = new(
        "NTC1025",
        "Icon-only NTButtonGroup item needs an accessible label",
        "NTButtonGroupItem requires a non-empty AriaLabel when rendering an icon-only item",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1025");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(TextSelectable, OpaqueBackground, TransparentBackground, InvisibleTextColor, TransparentSelectedBackground, InvisibleSelectedTextColor, MissingIconOnlyAriaLabel);
        registry.On("NTComponents.NTButtonGroup`1", AnalyzeGroup);
        registry.On("NTComponents.NTButtonGroupItem`1", AnalyzeItem);
    }

    private static void AnalyzeGroup(ComponentContext context) {
        var frame = context.Frame;
        var variant = frame.GetEnumName("Variant", "Tonal");
        // Null when the mode is unknown or undefined, which is neither selectable nor not selectable.
        var selectionMode = frame.GetEnumName("SelectionMode", "Single");
        var selectable = selectionMode is not (null or "None");

        if (variant == "Text" && selectable) {
            context.Report(TextSelectable, frame.Has("SelectionMode") ? frame.LocationOf("SelectionMode") : frame.LocationOf("Variant"));
        }

        if (variant is not null && frame.GetEnumName("BackgroundColor") is { } background) {
            var needsTransparent = variant is "Text" or "Outlined";
            if (needsTransparent && background != "Transparent") {
                context.Report(OpaqueBackground, frame.LocationOf("BackgroundColor"), variant);
            }
            else if (!needsTransparent && background == "Transparent") {
                context.Report(TransparentBackground, frame.LocationOf("BackgroundColor"), variant);
            }
        }

        if (frame.GetEnumName("TextColor") == "Transparent") {
            context.Report(InvisibleTextColor, frame.LocationOf("TextColor"));
        }

        // A selectable Text group already throws for NTC1019, so its selected background is not checked.
        if (selectable && variant is not (null or "Text") && frame.GetEnumName("SelectedBackgroundColor") == "Transparent") {
            context.Report(TransparentSelectedBackground, frame.LocationOf("SelectedBackgroundColor"), variant);
        }

        if (selectable && frame.GetEnumName("SelectedTextColor") == "Transparent") {
            context.Report(InvisibleSelectedTextColor, frame.LocationOf("SelectedTextColor"));
        }
    }

    private static void AnalyzeItem(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Has("Icon") && IsKnownIcon(frame.Attributes["Icon"].Value)
            && frame.GetStringState("Label") == StringState.Empty
            && frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(MissingIconOnlyAriaLabel, frame.LocationOf("AriaLabel"));
        }
    }

    /// <summary>True for a new icon or a static icon member of the library such as <c>MaterialIcon.Add</c>, which are never null.</summary>
    private static bool IsKnownIcon(IOperation? value) => OperationConstants.Unwrap(value) switch {
        IObjectCreationOperation => true,
        IPropertyReferenceOperation { Property: { IsStatic: true, ContainingAssembly.Name: "NTComponents" } } => true,
        IFieldReferenceOperation { Field: { IsStatic: true, ContainingAssembly.Name: "NTComponents" } } => true,
        _ => false
    };
}
