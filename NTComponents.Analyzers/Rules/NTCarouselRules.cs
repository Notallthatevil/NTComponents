using Microsoft.CodeAnalysis;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTCarousel and NTCarouselItem, mirroring the checks their OnParametersSet methods throw for.</summary>
internal static class NTCarouselRules {
    private static readonly string[] PositiveParameters = ["AutoPlayInterval", "ItemHeight", "MaxLargeItemWidth", "PreferredItemWidth"];

    public static readonly DiagnosticDescriptor EmptyCarouselAriaLabel = new(
        "NTC1069",
        "NTCarousel requires an accessible name",
        "NTCarousel AriaLabel cannot be empty because it provides the carousel accessible name",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1069");

    public static readonly DiagnosticDescriptor NonPositiveValue = new(
        "NTC1070",
        "NTCarousel numeric value must be positive",
        "NTCarousel {0} must be a finite value greater than zero",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1070");

    public static readonly DiagnosticDescriptor FullScreenSnapping = new(
        "NTC1071",
        "Full-screen NTCarousel requires snapping",
        "NTCarousel appearance 'FullScreen' requires EnableSnapping to remain enabled",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1071");

    public static readonly DiagnosticDescriptor EmptyItemAriaLabel = new(
        "NTC1072",
        "NTCarouselItem requires an accessible name",
        "NTCarouselItem AriaLabel cannot be empty because it provides the slide accessible name",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1072");

    public static readonly DiagnosticDescriptor InvalidItemAspectRatio = new(
        "NTC1073",
        "NTCarouselItem aspect ratio is outside the Material range",
        "NTCarouselItem AspectRatio must be finite and between 9:16 and 16:9",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1073");

    public static readonly DiagnosticDescriptor UndefinedAppearance = new(
        "NTC1074",
        "NTCarousel appearance must be defined",
        "NTCarousel Appearance must be a defined CarouselAppearance value",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1074");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(EmptyCarouselAriaLabel, NonPositiveValue, FullScreenSnapping, EmptyItemAriaLabel, InvalidItemAspectRatio, UndefinedAppearance);
        registry.On("NTComponents.NTCarousel", AnalyzeCarousel);
        registry.On("NTComponents.NTCarouselItem", AnalyzeItem);
    }

    private static void AnalyzeCarousel(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(EmptyCarouselAriaLabel, frame.LocationOf("AriaLabel"));
        }

        foreach (var name in PositiveParameters) {
            if (frame.GetNumber(name) is { } value && !(value > 0 && !double.IsInfinity(value))) {
                context.Report(NonPositiveValue, frame.LocationOf(name), name);
            }
        }

        if (frame.Attributes.TryGetValue("Appearance", out var appearance) && OperationConstants.TryGetEnum(appearance.Value, out var constant) && constant.Member is null) {
            context.Report(UndefinedAppearance, appearance.Location);
        }
        else if (frame.GetEnumName("Appearance", "MultiBrowse") == "FullScreen" && frame.GetBool("EnableSnapping", true) == false) {
            context.Report(FullScreenSnapping, frame.LocationOf("EnableSnapping"));
        }
    }

    private static void AnalyzeItem(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetStringState("AriaLabel") == StringState.Empty) {
            context.Report(EmptyItemAriaLabel, frame.LocationOf("AriaLabel"));
        }

        if (frame.GetNumber("AspectRatio") is { } ratio && !(ratio >= 9d / 16d && ratio <= 16d / 9d)) {
            context.Report(InvalidItemAspectRatio, frame.LocationOf("AspectRatio"));
        }
    }
}
