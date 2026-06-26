using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned range slider for selecting a lower and upper numeric value.
/// </summary>
/// <remarks>
///     The component renders two native <c>input type="range"</c> elements named as <c>{Field}.Start</c> and
///     <c>{Field}.End</c>, making it compatible with static server rendering form posts and normal keyboard slider
///     semantics. Material guidance recommends horizontal range sliders; vertical orientation is intentionally omitted.
/// </remarks>
/// <typeparam name="TNumber">The numeric value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders native range inputs for static SSR and enhances slider visuals with script.",
    CompatibilityDetails = "The component emits native range inputs and named values. Browser scripting keeps visual tracks and value indicators synchronized; live binding requires interactivity.")]
public partial class NTInputRangeSlider<TNumber> where TNumber : struct, INumber<TNumber> {
    private const string RangeSliderJsModulePath = "./_content/NTComponents/Form/NTInputRangeSlider.razor.js";
    private static readonly HashSet<string> ExplicitInputAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "data-nt-slider-range-start",
        "data-nt-slider-range-end",
        "name",
        "class",
        "type",
        "min",
        "max",
        "step",
        "value",
        "disabled",
        "autofocus",
        "aria-label",
        "aria-labelledby",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage",
        "oninput",
        "onchange",
        "style"
    };

    private IReadOnlyDictionary<string, object?>? _inputAttributes;
    private string? _elementStyle;
    private int[] _stopIndexes = [];

    /// <summary>
    ///     Gets the browser module path used for static SSR range-slider enhancement.
    /// </summary>
    protected string JsModulePath => RangeSliderJsModulePath;

    /// <summary>
    ///     Gets or sets a callback invoked after the range changes.
    /// </summary>
    [Parameter]
    public EventCallback<NTSliderRange<TNumber>> BindAfter { get; set; }

    /// <summary>
    ///     Gets or sets whether range changes are applied during native input instead of waiting for change.
    /// </summary>
    [Parameter]
    public bool BindOnInput { get; set; } = true;

    /// <summary>
    ///     Gets or sets an accessible label suffix for the upper-value handle.
    /// </summary>
    [Parameter]
    public string EndHandleLabel { get; set; } = "Maximum";

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
    ///     Gets or sets whether stop indicators are rendered.
    /// </summary>
    [Parameter]
    public bool ShowStops { get; set; }

    /// <summary>
    ///     Gets or sets whether current value indicators are rendered.
    /// </summary>
    [Parameter]
    public bool ShowValueIndicator { get; set; }

    /// <summary>
    ///     Gets or sets the Material 3 slider size preset.
    /// </summary>
    [Parameter]
    public NTSliderSize Size { get; set; }

    /// <summary>
    ///     Gets or sets an accessible label suffix for the lower-value handle.
    /// </summary>
    [Parameter]
    public string StartHandleLabel { get; set; } = "Minimum";

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
    ///     Gets the accessible label for the upper-value handle.
    /// </summary>
    protected string EndAriaLabel => BuildHandleAriaLabel(EndHandleLabel);

    /// <summary>
    ///     Gets the upper input id.
    /// </summary>
    protected string EndInputId => $"{InputId}-end";

    /// <summary>
    ///     Gets the upper input name.
    /// </summary>
    protected string? EndElementName => BuildRangeElementName("End");

    /// <summary>
    ///     Gets the upper static form-post input name.
    /// </summary>
    protected string? EndFormPostElementName => BuildRangeFormPostElementName("End");

    /// <summary>
    ///     Gets the effective maximum value.
    /// </summary>
    protected TNumber EffectiveMax => Max ?? TNumber.CreateChecked(100);

    /// <summary>
    ///     Gets the effective minimum value.
    /// </summary>
    protected TNumber EffectiveMin => Min ?? TNumber.Zero;

    /// <summary>
    ///     Gets the normalized lower selected value clamped to the effective slider range.
    /// </summary>
    protected TNumber EffectiveStart => NormalizeRange(CurrentValue).Start;

    /// <summary>
    ///     Gets the normalized upper selected value clamped to the effective slider range.
    /// </summary>
    protected TNumber EffectiveEnd => NormalizeRange(CurrentValue).End;

    /// <summary>
    ///     Gets the component style attribute.
    /// </summary>
    protected string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Gets the lower input event callback only when live input binding is enabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> StartInputEventCallback => BindOnInput ? EventCallback.Factory.Create<ChangeEventArgs>(this, OnStartInputAsync) : default;

    /// <summary>
    ///     Gets the lower change event callback only when live input binding is disabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> StartChangeEventCallback => BindOnInput ? default : EventCallback.Factory.Create<ChangeEventArgs>(this, OnStartChangeAsync);

    /// <summary>
    ///     Gets the upper input event callback only when live input binding is enabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> EndInputEventCallback => BindOnInput ? EventCallback.Factory.Create<ChangeEventArgs>(this, OnEndInputAsync) : default;

    /// <summary>
    ///     Gets the upper change event callback only when live input binding is disabled.
    /// </summary>
    protected EventCallback<ChangeEventArgs> EndChangeEventCallback => BindOnInput ? default : EventCallback.Factory.Create<ChangeEventArgs>(this, OnEndChangeAsync);

    /// <summary>
    ///     Gets filtered input attributes for the native range inputs.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? InputAttributes => _inputAttributes;

    /// <summary>
    ///     Gets whether hidden current-value form post inputs should render for read-only static SSR.
    /// </summary>
    protected bool ShouldRenderReadOnlyFormPostValue => !FieldDisabled && FieldReadOnly && !string.IsNullOrWhiteSpace(ElementName);

    /// <summary>
    ///     Gets whether stop indicators should render.
    /// </summary>
    protected bool ShouldRenderStops => ShowStops && _stopIndexes.Length > 0;

    /// <summary>
    ///     Gets the accessible label for the lower-value handle.
    /// </summary>
    protected string StartAriaLabel => BuildHandleAriaLabel(StartHandleLabel);

    /// <summary>
    ///     Gets the lower input style. The half-hit-target offset keeps the native thumb center aligned with the visible track percent.
    /// </summary>
    protected string StartInputStyle => "left:calc(var(--nt-slider-range-handle-hit-width) / -2);right:auto;width:calc(var(--nt-slider-end-percent) + var(--nt-slider-range-handle-hit-width));";

    /// <summary>
    ///     Gets the lower input id.
    /// </summary>
    protected string StartInputId => $"{InputId}-start";

    /// <summary>
    ///     Gets the lower input name.
    /// </summary>
    protected string? StartElementName => BuildRangeElementName("Start");

    /// <summary>
    ///     Gets the lower static form-post input name.
    /// </summary>
    protected string? StartFormPostElementName => BuildRangeFormPostElementName("Start");

    /// <summary>
    ///     Gets the upper input style. The half-hit-target offset keeps the native thumb center aligned with the visible track percent.
    /// </summary>
    protected string EndInputStyle => "left:calc(var(--nt-slider-start-percent) - var(--nt-slider-range-handle-hit-width) / 2);right:calc(var(--nt-slider-range-handle-hit-width) / -2);width:auto;";

    /// <summary>
    ///     Gets stop indicator indexes.
    /// </summary>
    protected IEnumerable<int> StopIndexes => _stopIndexes;

    /// <summary>
    ///     Gets the native step attribute value.
    /// </summary>
    protected string? StepAttribute => string.IsNullOrWhiteSpace(Step) ? null : Step;

    /// <inheritdoc />
    protected override string InputIdPrefix => "nt-range-slider";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _inputAttributes = BuildFilteredAttributes(ExplicitInputAttributeNames);
        _stopIndexes = BuildStopIndexes();
        _elementStyle = BuildElementStyle();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out NTSliderRange<TNumber> result, [NotNullWhen(false)] out string? validationErrorMessage) {
        throw new NotSupportedException($"{GetType().Name} uses two native range inputs instead of parsing a single string value.");
    }

    /// <summary>
    ///     Builds the root class for the current state.
    /// </summary>
    /// <returns>The root CSS class.</returns>
    protected string BuildRootClass() {
        var builder = new StringBuilder("nt-slider nt-slider-range nt-slider-horizontal");
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

        if (FieldDisabled) {
            builder.Append(" nt-slider-disabled");
        }

        if (FieldReadOnly) {
            builder.Append(" nt-slider-readonly");
        }

        return AppendFieldCssClass(builder.ToString());
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
        var startPercent = CalculatePercent(EffectiveStart);
        var endPercent = CalculatePercent(EffectiveEnd);
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

    private async Task OnStartChangeAsync(ChangeEventArgs args) {
        if (BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateStartValueAsync(args.Value?.ToString());
    }

    private async Task OnStartInputAsync(ChangeEventArgs args) {
        if (!BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateStartValueAsync(args.Value?.ToString());
    }

    private async Task OnEndChangeAsync(ChangeEventArgs args) {
        if (BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateEndValueAsync(args.Value?.ToString());
    }

    private async Task OnEndInputAsync(ChangeEventArgs args) {
        if (!BindOnInput || FieldReadOnly || FieldDisabled) {
            return;
        }

        await UpdateEndValueAsync(args.Value?.ToString());
    }

    private async Task UpdateStartValueAsync(string? value) {
        if (!TryParseNumber(value, out var parsedValue)) {
            return;
        }

        var end = EffectiveEnd;
        var start = ClampValue(parsedValue);
        CurrentValue = new NTSliderRange<TNumber>(start > end ? end : start, end);
        await BindAfter.InvokeAsync(CurrentValue);
    }

    private async Task UpdateEndValueAsync(string? value) {
        if (!TryParseNumber(value, out var parsedValue)) {
            return;
        }

        var start = EffectiveStart;
        var end = ClampValue(parsedValue);
        CurrentValue = new NTSliderRange<TNumber>(start, end < start ? start : end);
        await BindAfter.InvokeAsync(CurrentValue);
    }

    private bool TryParseNumber(string? value, out TNumber result) => TNumber.TryParse(value, CultureInfo.InvariantCulture, out result);

    private string? BuildElementStyle() {
        var startPercent = CalculatePercent(EffectiveStart);
        var endPercent = CalculatePercent(EffectiveEnd);

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
            .AddVariable("nt-slider-start-gap", "8px")
            .AddVariable("nt-slider-end-gap", "8px")
            .Build();
    }

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

    private string BuildHandleAriaLabel(string handleLabel) {
        if (string.IsNullOrWhiteSpace(Label)) {
            return handleLabel;
        }

        return $"{Label} {handleLabel}";
    }

    private string? BuildRangeElementName(string suffix) => FieldReadOnly ? null : BuildRangeFormPostElementName(suffix);

    private string? BuildRangeFormPostElementName(string suffix) => string.IsNullOrWhiteSpace(ElementName) ? null : $"{ElementName}.{suffix}";

    private NTSliderRange<TNumber> NormalizeRange(NTSliderRange<TNumber> range) {
        var start = ClampValue(range.Start);
        var end = ClampValue(range.End);
        return start <= end
            ? new NTSliderRange<TNumber>(start, end)
            : new NTSliderRange<TNumber>(end, start);
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
