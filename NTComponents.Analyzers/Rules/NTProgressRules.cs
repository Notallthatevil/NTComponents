using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTProgress and NTLoader, which fall back to runtime defaults for out-of-range values and render an accessible name from AriaLabel or AriaLabelledBy.</summary>
internal static class NTProgressRules {
    private const double MinimumLoaderDurationMilliseconds = 400;

    public static readonly DiagnosticDescriptor NonPositiveMax = new(
        "NTC1052",
        "NTProgress Max must be positive",
        "NTProgress Max should be a positive value; non-positive values fall back to 100",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1052");

    public static readonly DiagnosticDescriptor OutOfRangeValue = new(
        "NTC1053",
        "NTProgress Value should be within range",
        "NTProgress Value should be between 0 and Max; out-of-range values are clamped at render time",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1053");

    public static readonly DiagnosticDescriptor ShortLoaderAnimation = new(
        "NTC1054",
        "NTLoader animation duration is too short",
        "NTLoader AnimationDuration should be at least 400ms; shorter durations are clamped",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1054");

    public static readonly DiagnosticDescriptor SingleShapeLoader = new(
        "NTC1055",
        "Animated NTLoader needs multiple shapes",
        "Animated NTLoader Shapes should contain at least two shapes; a single-shape sequence cannot visibly morph",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1055");

    public static readonly DiagnosticDescriptor MissingAccessibleName = new(
        "NTC1110",
        "Progress indicator has no accessible name",
        "{0} has no accessible name; set a non-empty AriaLabel or AriaLabelledBy",
        RuleCategory.Accessibility,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1110");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(NonPositiveMax, OutOfRangeValue, ShortLoaderAnimation, SingleShapeLoader, MissingAccessibleName);
        registry.On("NTComponents.NTProgress", AnalyzeProgress);
        registry.On("NTComponents.NTProgress", AnalyzeAccessibleName);
        registry.On("NTComponents.NTLoader", AnalyzeLoader);
        registry.On("NTComponents.NTLoader", AnalyzeAccessibleName);
    }

    private static void AnalyzeProgress(ComponentContext context) {
        var frame = context.Frame;
        var max = frame.GetNumber("Max");
        if (max <= 0) {
            context.Report(NonPositiveMax, frame.LocationOf("Max"));
            return;
        }

        // An omitted Max is 100; a Max that is supplied but not constant leaves the range unknown.
        double? effectiveMax = frame.Has("Max") ? max : frame.HasSplattedAttributes ? null : 100;
        if (frame.GetNumber("Value") is { } value && effectiveMax is { } upper && (value < 0 || value > upper)) {
            context.Report(OutOfRangeValue, frame.LocationOf("Value"));
        }
    }

    private static void AnalyzeLoader(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Attributes.TryGetValue("AnimationDuration", out var duration) && TryGetMilliseconds(duration.Value, out var milliseconds) && Math.Round(milliseconds) < MinimumLoaderDurationMilliseconds) {
            context.Report(ShortLoaderAnimation, duration.Location);
        }

        // Shapes with a single entry cannot morph; an empty list falls back to the default sequence.
        if (frame.GetBool("Animate", true) == true && frame.Attributes.TryGetValue("Shapes", out var shapes) && TryGetElementCount(shapes.Value) == 1) {
            context.Report(SingleShapeLoader, shapes.Location);
        }
    }

    /// <summary>
    ///     The component renders aria-label unless a non-empty aria-labelledby exists, and either one can come from the matching
    ///     parameter or from a raw attribute that wins over it. AriaLabel defaults to "Loading".
    /// </summary>
    private static void AnalyzeAccessibleName(ComponentContext context) {
        var frame = context.Frame;
        if (frame.HasSplattedAttributes
            || EffectiveState(frame, "aria-labelledby", "AriaLabelledBy", null) != StringState.Empty
            || EffectiveState(frame, "aria-label", "AriaLabel", "Loading") != StringState.Empty) {
            return;
        }

        context.Report(MissingAccessibleName, frame.Has("aria-label") ? frame.LocationOf("aria-label") : frame.LocationOf("AriaLabel"), frame.Type.Name);
    }

    private static StringState EffectiveState(ComponentFrame frame, string attributeName, string parameterName, string? parameterDefault) =>
        frame.Attributes.TryGetValue(attributeName, out var attribute) && !OperationConstants.IsNull(attribute.Value)
            ? frame.GetStringState(attributeName)
            : frame.GetStringState(parameterName, parameterDefault);

    /// <summary>Reads <c>TimeSpan.FromMilliseconds</c>, <c>FromSeconds</c>, <c>TimeSpan.Zero</c> and <c>new TimeSpan(...)</c> with constant arguments.</summary>
    private static bool TryGetMilliseconds(IOperation? operation, out double milliseconds) {
        milliseconds = 0;
        switch (OperationConstants.Unwrap(operation)) {
            case IFieldReferenceOperation { Field: { Name: "Zero", ContainingType: { } owner } } when IsTimeSpan(owner):
                return true;
            case IInvocationOperation { TargetMethod: { Name: "FromMilliseconds" or "FromSeconds" } method } invocation
                when IsTimeSpan(method.ContainingType) && invocation.Arguments.Length > 0 && invocation.Arguments.Skip(1).All(static argument => argument.ArgumentKind == ArgumentKind.DefaultValue)
                    && OperationConstants.TryGetNumber(invocation.Arguments[0].Value, out var amount):
                milliseconds = method.Name == "FromSeconds" ? amount * 1000 : amount;
                return true;
            case IObjectCreationOperation creation when IsTimeSpan(creation.Type) && TryGetConstructorMilliseconds(creation, out milliseconds):
                return true;
            default:
                return false;
        }
    }

    /// <summary>The constructor overloads are (ticks), (hours, minutes, seconds), (days, hours, minutes, seconds) and (days, hours, minutes, seconds, milliseconds).</summary>
    private static bool TryGetConstructorMilliseconds(IObjectCreationOperation creation, out double milliseconds) {
        milliseconds = 0;
        var values = new double[creation.Arguments.Length];
        for (var i = 0; i < values.Length; i++) {
            if (!OperationConstants.TryGetNumber(creation.Arguments[i].Value, out values[i])) {
                return false;
            }
        }

        switch (values.Length) {
            case 1:
                milliseconds = values[0] / TimeSpan.TicksPerMillisecond;
                return true;
            case 3:
                milliseconds = ((values[0] * 3600) + (values[1] * 60) + values[2]) * 1000;
                return true;
            case 4:
            case 5:
                milliseconds = (((values[0] * 86400) + (values[1] * 3600) + (values[2] * 60) + values[3]) * 1000) + (values.Length == 5 ? values[4] : 0);
                return true;
            default:
                return false;
        }
    }

    private static bool IsTimeSpan(ITypeSymbol? type) => type?.ToDisplayString() == "System.TimeSpan";

    /// <summary>Counts the elements of an array or collection expression, or returns null when the count is unknown (spreads, variables).</summary>
    private static int? TryGetElementCount(IOperation? operation) => OperationConstants.Unwrap(operation) switch {
        IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues.Length,
        ICollectionExpressionOperation collection when !collection.Elements.Any(static element => element is ISpreadOperation) => collection.Elements.Length,
        _ => null
    };
}
