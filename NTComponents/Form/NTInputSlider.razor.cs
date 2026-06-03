using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned slider for selecting one numeric value from a bounded range.
/// </summary>
/// <remarks>
///     Use this component for standard and centered sliders. The component renders a native <c>input type="range"</c>,
///     so static server rendering form posts, keyboard navigation, and assistive technology slider semantics work without
///     JavaScript. Set <see cref="BindOnInput" /> to <see langword="false" /> only when updates should wait until change.
/// </remarks>
/// <typeparam name="TNumber">The numeric value type.</typeparam>
public partial class NTInputSlider<TNumber> where TNumber : struct, INumber<TNumber> {
    private const string SliderJsModulePath = "./_content/NTComponents/Form/NTInputSlider.razor.js";
    private static readonly HashSet<string> ExplicitInputAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "data-nt-slider-input",
        "name",
        "type",
        "min",
        "max",
        "step",
        "value",
        "disabled",
        "autofocus",
        "aria-labelledby",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "oninput",
        "onchange"
    };

    private IReadOnlyDictionary<string, object?>? _inputAttributes;
    private string? _elementStyle;
    private int[] _stopIndexes = [];

    /// <summary>
    ///     Gets the browser module path used for static SSR slider enhancement.
    /// </summary>
    protected string JsModulePath => SliderJsModulePath;

    /// <summary>
    ///     Gets or sets a callback invoked after the value changes.
    /// </summary>
    [Parameter]
    public EventCallback<TNumber> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets whether value changes are applied during native input instead of waiting for change.
    /// </summary>
    [Parameter]
    public bool BindOnInput { get; set; } = true;

    /// <summary>
    ///     Gets or sets the optional icon rendered inside the track for supported Material 3 slider configurations.
    /// </summary>
    [Parameter]
    public TnTIcon? InsetIcon { get; set; }

    /// <summary>
    ///     Gets or sets the maximum slider value. Defaults to 100.
    /// </summary>
    [Parameter]
    public TNumber? Max { get; set; }

    /// <summary>
    ///     Gets or sets the minimum slider value. Defaults to 0.
    /// </summary>
    [Parameter]
    public TNumber? Min { get; set; }

    /// <summary>
    ///     Gets or sets slider orientation.
    /// </summary>
    [Parameter]
    public NTSliderOrientation Orientation { get; set; }

    /// <summary>
    ///     Gets or sets whether stop indicators are rendered.
    /// </summary>
    [Parameter]
    public bool ShowStops { get; set; }

    /// <summary>
    ///     Gets or sets whether the current value indicator is rendered.
    /// </summary>
    [Parameter]
    public bool ShowValueIndicator { get; set; }

    /// <summary>
    ///     Gets or sets the Material 3 slider size preset.
    /// </summary>
    [Parameter]
    public NTSliderSize Size { get; set; }

    /// <summary>
    ///     Gets or sets the native range step. Use <c>"any"</c> for continuous values.
    /// </summary>
    [Parameter]
    public string? Step { get; set; } = "1";

    /// <summary>
    ///     Gets or sets the number of stop indicators to draw. When omitted, the component derives a modest count from <see cref="Step" />.
    /// </summary>
    [Parameter]
    public int? StopCount { get; set; }

    /// <summary>
    ///     Gets or sets the single-value slider variant.
    /// </summary>
    [Parameter]
    public NTSliderVariant Variant { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for active track color.
    /// </summary>
    [Parameter]
    public TnTColor? ActiveTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for disabled slider color.
    /// </summary>
    [Parameter]
    public TnTColor? DisabledColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for error color.
    /// </summary>
    [Parameter]
    public TnTColor? ErrorColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for focus color.
    /// </summary>
    [Parameter]
    public TnTColor? FocusColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for handle color.
    /// </summary>
    [Parameter]
    public TnTColor? HandleColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for inactive track color.
    /// </summary>
    [Parameter]
    public TnTColor? InactiveTrackColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for label text color.
    /// </summary>
    [Parameter]
    public TnTColor? LabelColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for state-layer color.
    /// </summary>
    [Parameter]
    public TnTColor? StateLayerColor { get; set; }

    /// <summary>
    ///     Gets or sets an optional override for supporting text color.
    /// </summary>
    [Parameter]
    public TnTColor? SupportingTextColor { get; set; }

    /// <summary>
    ///     Gets the effective maximum value.
    /// </summary>
    protected TNumber EffectiveMax => Max ?? TNumber.CreateChecked(100);

    /// <summary>
    ///     Gets the effective minimum value.
    /// </summary>
    protected TNumber EffectiveMin => Min ?? TNumber.Zero;

    /// <summary>
    ///     Gets the selected value clamped to the effective slider range.
    /// </summary>
    protected TNumber EffectiveValue => ClampValue(CurrentValue);

    /// <summary>
    ///     Gets the native ARIA orientation value for vertical sliders.
    /// </summary>
    protected string? AriaOrientation => Orientation == NTSliderOrientation.Vertical ? "vertical" : null;

    /// <summary>
    ///     Gets the component style attribute.
    /// </summary>
    protected string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Gets the input event callback only when live input binding is enabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> InputEventCallback => BindOnInput ? EventCallback.Factory.Create<ChangeEventArgs>(this, OnInputAsync) : default;

    /// <summary>
    ///     Gets the change event callback only when live input binding is disabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> ChangeEventCallback => BindOnInput ? default : EventCallback.Factory.Create<ChangeEventArgs>(this, OnChangeAsync);

    /// <summary>
    ///     Gets the inset icon class for the current placement.
    /// </summary>
    protected string InsetIconClass => CalculatePercent(EffectiveValue) >= 24
        ? "nt-slider-inset-icon nt-slider-inset-icon-active"
        : "nt-slider-inset-icon nt-slider-inset-icon-inactive";

    /// <summary>
    ///     Gets filtered input attributes.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? InputAttributes => _inputAttributes;

    /// <summary>
    ///     Gets the visible input name. Read-only sliders post through a hidden field because range inputs do not support readonly.
    /// </summary>
    protected string? InputElementName => FieldReadOnly ? null : ElementName;

    /// <summary>
    ///     Gets the label id reference for the native slider.
    /// </summary>
    protected string? LabelReference => HasLabel ? LabelId : null;

    /// <summary>
    ///     Gets whether hidden current-value form post input should render for read-only static SSR.
    /// </summary>
    protected bool ShouldRenderReadOnlyFormPostValue => !FieldDisabled && FieldReadOnly && !string.IsNullOrWhiteSpace(ElementName);

    /// <summary>
    ///     Gets whether the inset icon is valid for the current Material slider configuration.
    /// </summary>
    protected bool ShouldRenderInsetIcon => InsetIcon is not null
        && Variant == NTSliderVariant.Standard
        && Orientation == NTSliderOrientation.Horizontal
        && Size is NTSliderSize.Medium or NTSliderSize.Large or NTSliderSize.ExtraLarge;

    /// <summary>
    ///     Gets whether stop indicators should render.
    /// </summary>
    protected bool ShouldRenderStops => ShowStops && _stopIndexes.Length > 0;

    /// <summary>
    ///     Gets stop indicator indexes.
    /// </summary>
    protected IEnumerable<int> StopIndexes => _stopIndexes;

    /// <summary>
    ///     Gets the native step attribute value.
    /// </summary>
    protected string? StepAttribute => string.IsNullOrWhiteSpace(Step) ? null : Step;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-slider";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _inputAttributes = BuildFilteredAttributes(ExplicitInputAttributeNames);
        _stopIndexes = BuildStopIndexes();
        _elementStyle = BuildElementStyle();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TNumber result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (TNumber.TryParse(value, CultureInfo.InvariantCulture, out var parsedValue)) {
            result = parsedValue;
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, "The {0} field must be a number.", DisplayName ?? FieldIdentifier.FieldName);
        return false;
    }

    /// <summary>
    ///     Builds the root class for the current state.
    /// </summary>
    /// <param name="hasErrorText">Whether the field is invalid.</param>
    /// <returns>The root CSS class.</returns>
    protected string BuildRootClass(bool hasErrorText) {
        var builder = new StringBuilder("nt-slider");
        builder.Append(Variant == NTSliderVariant.Centered ? " nt-slider-centered" : " nt-slider-standard-variant");
        builder.Append(Orientation == NTSliderOrientation.Vertical ? " nt-slider-vertical" : " nt-slider-horizontal");
        builder.Append(Size switch {
            NTSliderSize.Small => " nt-slider-small",
            NTSliderSize.Medium => " nt-slider-medium",
            NTSliderSize.Large => " nt-slider-large",
            NTSliderSize.ExtraLarge => " nt-slider-extra-large",
            _ => " nt-slider-extra-small"
        });

        if (ShowStops) {
            builder.Append(" nt-slider-with-stops");
        }

        if (ShowValueIndicator) {
            builder.Append(" nt-slider-with-value-indicator");
        }

        if (ShouldRenderInsetIcon) {
            builder.Append(" nt-slider-with-inset-icon");
        }

        if (hasErrorText) {
            builder.Append(" nt-slider-invalid");
        }

        if (FieldDisabled) {
            builder.Append(" nt-slider-disabled");
        }

        if (FieldReadOnly) {
            builder.Append(" nt-slider-readonly");
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Builds an inline style for a stop indicator.
    /// </summary>
    /// <param name="stopIndex">The stop index.</param>
    /// <returns>The style attribute.</returns>
    protected string BuildStopStyle(int stopIndex) => $"--nt-slider-stop-percent:{FormatPercent(CalculateStopPercent(stopIndex, _stopIndexes.Length))};";

    /// <summary>
    ///     Builds a stop indicator class that matches its active or inactive track segment.
    /// </summary>
    /// <param name="stopIndex">The stop index.</param>
    /// <returns>The stop CSS class.</returns>
    protected string BuildStopClass(int stopIndex) {
        var stopPercent = CalculateStopPercent(stopIndex, _stopIndexes.Length);
        var startPercent = GetActiveStartPercent();
        var endPercent = GetActiveEndPercent();
        return stopPercent >= startPercent && stopPercent <= endPercent
            ? "nt-slider-stop nt-slider-stop-active"
            : "nt-slider-stop nt-slider-stop-inactive";
    }

    /// <summary>
    ///     Formats a value for a native attribute.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The invariant string value.</returns>
    protected static string FormatValue(TNumber value) => value.ToString(null, CultureInfo.InvariantCulture);

    private async Task OnChangeAsync(ChangeEventArgs args) {
        if (BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateValueAsync(args.Value?.ToString());
    }

    private async Task OnInputAsync(ChangeEventArgs args) {
        if (!BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateValueAsync(args.Value?.ToString());
    }

    private async Task UpdateValueAsync(string? value) {
        if (!TryParseValueFromString(value, out var parsedValue, out _)) {
            return;
        }

        CurrentValue = ClampValue(parsedValue);
        await BindAfter.InvokeAsync(CurrentValue);
    }

    private string? BuildElementStyle() {
        var startPercent = GetActiveStartPercent();
        var endPercent = GetActiveEndPercent();
        var startGap = GetActiveStartGap(startPercent);
        var endGap = GetActiveEndGap(endPercent);

        return CssStyleBuilder.Create()
            .AddVariable("nt-slider-active-track-color", ActiveTrackColor.ToCssTnTColorVariable(), ActiveTrackColor.HasValue)
            .AddVariable("nt-slider-disabled-color", DisabledColor.ToCssTnTColorVariable(), DisabledColor.HasValue)
            .AddVariable("nt-slider-error-color", ErrorColor.ToCssTnTColorVariable(), ErrorColor.HasValue)
            .AddVariable("nt-slider-focus-color", FocusColor.ToCssTnTColorVariable(), FocusColor.HasValue)
            .AddVariable("nt-slider-handle-color", HandleColor.ToCssTnTColorVariable(), HandleColor.HasValue)
            .AddVariable("nt-slider-inactive-track-color", InactiveTrackColor.ToCssTnTColorVariable(), InactiveTrackColor.HasValue)
            .AddVariable("nt-slider-label-color", LabelColor.ToCssTnTColorVariable(), LabelColor.HasValue)
            .AddVariable("nt-slider-state-layer-color", StateLayerColor.ToCssTnTColorVariable(), StateLayerColor.HasValue)
            .AddVariable("nt-slider-supporting-text-color", SupportingTextColor.ToCssTnTColorVariable(), SupportingTextColor.HasValue)
            .AddVariable("nt-slider-start-percent", FormatPercent(startPercent))
            .AddVariable("nt-slider-end-percent", FormatPercent(endPercent))
            .AddVariable("nt-slider-start-gap", startGap)
            .AddVariable("nt-slider-end-gap", endGap)
            .AddVariable("nt-slider-inset-icon-position", GetInsetIconPosition())
            .Build();
    }

    private double GetActiveStartPercent() {
        var valuePercent = CalculatePercent(EffectiveValue);
        return Variant == NTSliderVariant.Centered ? Math.Min(50, valuePercent) : 0;
    }

    private double GetActiveEndPercent() {
        var valuePercent = CalculatePercent(EffectiveValue);
        return Variant == NTSliderVariant.Centered ? Math.Max(50, valuePercent) : valuePercent;
    }

    private string GetActiveStartGap(double startPercent) {
        if (Variant != NTSliderVariant.Centered) {
            return "0px";
        }

        return startPercent < 50 ? "8px" : "0px";
    }

    private string GetActiveEndGap(double endPercent) {
        if (Variant != NTSliderVariant.Centered) {
            return "8px";
        }

        return endPercent > 50 ? "8px" : "0px";
    }

    private string GetInsetIconPosition() => CalculatePercent(EffectiveValue) >= 24 ? "16px" : "calc(var(--nt-slider-end-percent) + 20px)";

    private int[] BuildStopIndexes() {
        var count = StopCount ?? EstimateStopCount();
        if (count < 2) {
            return [];
        }

        count = Math.Min(count, 50);
        return Enumerable.Range(0, count).ToArray();
    }

    private int EstimateStopCount() {
        if (!ShowStops || string.Equals(Step, "any", StringComparison.OrdinalIgnoreCase)) {
            return 0;
        }

        if (!double.TryParse(Step, NumberStyles.Float, CultureInfo.InvariantCulture, out var step) || step <= 0) {
            return 0;
        }

        var span = Math.Abs(ToDouble(EffectiveMax) - ToDouble(EffectiveMin));
        var count = (int)Math.Floor(span / step) + 1;
        return count is >= 2 and <= 20 ? count : 2;
    }

    private double CalculatePercent(TNumber value) {
        var min = ToDouble(EffectiveMin);
        var max = ToDouble(EffectiveMax);
        if (max <= min) {
            return 0;
        }

        return Math.Clamp((ToDouble(value) - min) / (max - min) * 100, 0, 100);
    }

    private static double CalculateStopPercent(int stopIndex, int stopCount) => stopCount <= 1 ? 0 : (double)stopIndex / (stopCount - 1) * 100;

    private static string FormatPercent(double percent) => string.Create(CultureInfo.InvariantCulture, $"{percent:0.###}%");

    private static double ToDouble(TNumber value) => double.CreateChecked(value);

    private TNumber ClampValue(TNumber value) {
        if (EffectiveMax <= EffectiveMin) {
            return EffectiveMin;
        }

        if (value < EffectiveMin) {
            return EffectiveMin;
        }

        return value > EffectiveMax ? EffectiveMax : value;
    }
}
