using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>Form parameters that are ignored, clamped or normalized instead of rejected. Reports through <see cref="CommonRules" />.</summary>
internal static class NTInputAdjustmentRules {
    public static void Register(ComponentRuleRegistry registry) {
        registry.On("NTComponents.NTTextArea", AnalyzeTextArea);
        registry.On("NTComponents.NTInputDateTime`1", AnalyzeDateTime);
        registry.On("NTComponents.NTInputSlider`1", AnalyzeSliderStops);
        registry.On("NTComponents.NTInputRangeSlider`1", AnalyzeSliderStops);
        registry.On("NTComponents.NTFormFieldGridView", AnalyzeMaxColumns);
        registry.On("NTComponents.NTFormSectionView", AnalyzeMaxColumns);
        registry.On("NTComponents.NTFormFieldLayoutSpan", AnalyzeLayoutSpan);
        registry.On("NTComponents.NTRichTextEditor", AnalyzeRichTextEditorAliases);
    }

    // AutoGrow defaults to true, so only an explicit true is worth pointing out.
    private static void AnalyzeTextArea(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Has("AutoGrow") && frame.GetBool("AutoGrow") == true && frame.GetBool("SizeByContent") == true) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("AutoGrow"), "NTTextArea AutoGrow", "when SizeByContent is true");
        }
    }

    // MonthOnly is applied only when the underlying type is DateOnly. Unsupported types are reported by NTC1106 instead.
    private static void AnalyzeDateTime(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetBool("MonthOnly") == true
            && NTInputRules.SingleTypeArgument(frame) is { } argument
            && NTInputRules.IsSystemType(NTInputRules.UnderlyingType(argument), "DateTime", "DateTimeOffset", "TimeOnly")) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("MonthOnly"), "NTInputDateTime MonthOnly", "unless T is DateOnly");
        }
    }

    // Stops render only when ShowStops is true, and the count is limited to 50; fewer than 2 draws none.
    private static void AnalyzeSliderStops(ComponentContext context) {
        var frame = context.Frame;
        if (!frame.HasNonNull("StopCount")) {
            return;
        }

        var parameter = frame.Type.Name + " StopCount";
        var showStops = frame.GetBool("ShowStops");
        if (showStops == false) {
            context.Report(CommonRules.NoEffect, frame.LocationOf("StopCount"), parameter, "unless ShowStops is true");
        }
        else if (showStops == true && frame.GetNumber("StopCount") is { } count && (count > 50 || count < 2)) {
            context.Report(CommonRules.Adjusted, frame.LocationOf("StopCount"), parameter, Format(count), count > 50 ? "50" : "0");
        }
    }

    // NTFormFieldGridView.NormalizeMaxColumns allows 1, 2, 3, 4 and 6. NTFormSectionView passes its value straight to the grid.
    private static void AnalyzeMaxColumns(ComponentContext context) {
        var frame = context.Frame;
        if (frame.GetNumber("MaxColumns") is not { } value) {
            return;
        }

        var normalized = value switch {
            <= 1 => 1,
            4 or 5 => 4,
            > 6 => 6,
            _ => value
        };
        if (normalized != value) {
            context.Report(CommonRules.Adjusted, frame.LocationOf("MaxColumns"), frame.Type.Name + " MaxColumns", Format(value), Format(normalized));
        }
    }

    private static void AnalyzeLayoutSpan(ComponentContext context) {
        var frame = context.Frame;
        foreach (var name in new[] { "SmallColumns", "MediumColumns", "LargeColumns" }) {
            if (frame.GetNumber(name) is { } columns && (columns < 1 || columns > 12)) {
                context.Report(CommonRules.Adjusted, frame.LocationOf(name), "NTFormFieldLayoutSpan " + name, Format(columns), columns < 1 ? "1" : "12");
            }
        }
    }

    // OnParametersSet copies an alias only when the canonical parameter is null, so a non-null canonical value silently wins.
    private static void AnalyzeRichTextEditorAliases(ComponentContext context) {
        var frame = context.Frame;
        foreach (var (alias, canonical) in new[] { ("ErrorMessage", "ErrorText"), ("StartIcon", "LeadingIcon"), ("EndIcon", "TrailingIcon") }) {
            if (frame.HasNonNull(alias) && frame.Has(canonical) && IsKnownNonNull(frame.Attributes[canonical].Value)) {
                context.Report(CommonRules.NoEffect, frame.LocationOf(alias), "NTRichTextEditor " + alias, $"when {canonical} is also set");
            }
        }
    }

    /// <summary>True for a non-null constant, a new object, or a static member that is not annotated as nullable.</summary>
    private static bool IsKnownNonNull(IOperation? value) => OperationConstants.Unwrap(value) switch {
        { ConstantValue: { HasValue: true, Value: var constant } } => constant is not null,
        IObjectCreationOperation => true,
        IPropertyReferenceOperation { Property: { IsStatic: true, NullableAnnotation: not NullableAnnotation.Annotated } } => true,
        IFieldReferenceOperation { Field: { IsStatic: true, NullableAnnotation: not NullableAnnotation.Annotated } } => true,
        _ => false
    };

    private static string Format(double number) => number.ToString(CultureInfo.InvariantCulture);
}
