using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTCard, which ignores a transparent background outside the Outlined variant and Elevation outside the Elevated variant.</summary>
internal static class NTCardRules {

    public static readonly DiagnosticDescriptor TransparentBackground = new(
        "NTC1001",
        "Transparent background is invalid for this NTCard variant",
        "NTCard variant '{0}' cannot use a transparent BackgroundColor. Use Outlined or remove the override.",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1001");

    public static readonly DiagnosticDescriptor IgnoredElevation = new(
        "NTC1002",
        "NTCard elevation is ignored for this variant",
        "NTCard Elevation only applies when Variant is Elevated. Current variant is '{0}'.",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1002");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(TransparentBackground, IgnoredElevation);
        registry.On("NTComponents.NTCard", Analyze);
    }

    private static void Analyze(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetEnumName("Variant", "Filled") is { } variant) {
            // BackgroundColor is nullable: null (or omitted) means "use the variant default", which GetEnumName reports as null.
            if (variant != "Outlined" && frame.GetEnumName("BackgroundColor") == "Transparent") {
                context.Report(TransparentBackground, frame.LocationOf("BackgroundColor"), variant);
            }

            if (variant != "Elevated" && frame.Has("Elevation")) {
                context.Report(IgnoredElevation, frame.LocationOf("Elevation"), variant);
            }
        }
    }
}
