using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned native date/time input.
/// </summary>
/// <typeparam name="DateTimeType">The date/time value type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders native date and time input markup and enhances custom picker behavior with script.",
    CompatibilityDetails = "Static SSR emits native date/time-compatible input attributes for form posts. The custom picker, live parsing, and validation updates require browser or Blazor enhancement.")]
public partial class NTInputDateTime<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] DateTimeType> {

    private const string JsModulePath = "./_content/NTComponents/Form/NTInputDateTime.razor.js";
    private static readonly Type _underlyingDateTimeType = Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType);

    private IReadOnlyDictionary<string, object?>? _additionalInputAttributes;
    private string? _additionalInputAttributesFormat;
    private string? _additionalInputAttributesPickerId;
    private string? _additionalInputAttributesPickerMode;
    private bool _additionalInputAttributesCustomPicker;
    private DateTimeInputMetadata _metadata = default!;
    private bool _metadataMonthOnly;
    private bool _metadataResolved;
    private string _effectiveFormat = string.Empty;
    private TnTIcon? _filledPickerIcon;
    private string? _filledPickerIconName;
    private TnTColor _filledPickerIconColor;
    private IconSize _filledPickerIconSize;
    private RenderFragment? _filledPickerIconTooltip;
    private TnTIcon _resolvedPickerTriggerIcon = MaterialIcon.CalendarMonth;
    private string _pickerClass = string.Empty;
    private string _pickerHeadlineId = string.Empty;
    private string _pickerId = string.Empty;
    private string _rootClass = "nt-input-date-time nt-input-date-time-standard";

    private string RootClass => AppendFieldCssClass(_rootClass);

    /// <summary>
    ///     Gets or sets a value indicating whether the Material 3 aligned custom picker should be rendered.
    /// </summary>
    /// <remarks>
    ///     When enabled, the native input still posts and binds the value while the custom picker provides the docked or
    ///     modal visual interaction. On small screens and down, the picker is presented as a centered modal dialog.
    /// </remarks>
    [Parameter]
    public bool EnableCustomPicker { get; set; }

    /// <summary>
    ///     Gets or sets the format string used to display the date/time value.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether <see cref="DateOnly" /> should render as a month input.
    /// </summary>
    [Parameter]
    public bool MonthOnly { get; set; }

    /// <summary>
    ///     Gets or sets the icon used by the custom picker trigger.
    /// </summary>
    [Parameter]
    public TnTIcon? PickerTriggerIcon { get; set; }

    /// <summary>
    ///     Gets the resolved legacy input type.
    /// </summary>
    public InputType Type => _metadata.Type;

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => _metadata.Type;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        var pickerId = BuildPickerId();
        if (_additionalInputAttributes is not null
            && _additionalInputAttributesCustomPicker == EnableCustomPicker
            && _additionalInputAttributesFormat == _effectiveFormat
            && _additionalInputAttributesPickerId == pickerId
            && _additionalInputAttributesPickerMode == _metadata.PickerMode) {
            return _additionalInputAttributes;
        }

        var attributes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["format"] = _effectiveFormat
        };
        if (EnableCustomPicker) {
            attributes["data-tnt-dtp-input"] = "true";
            attributes["data-tnt-dtp-target"] = pickerId;
            attributes["data-tnt-dtp-mode"] = _metadata.PickerMode;
            attributes["data-tnt-dtp-open-on-focus"] = "true";
        }

        _additionalInputAttributes = attributes;
        _additionalInputAttributesCustomPicker = EnableCustomPicker;
        _additionalInputAttributesFormat = _effectiveFormat;
        _additionalInputAttributesPickerId = pickerId;
        _additionalInputAttributesPickerMode = _metadata.PickerMode;
        return _additionalInputAttributes;
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(DateTimeType? value) {
        return value switch {
            DateTime dateTimeValue => BindConverter.FormatValue(dateTimeValue, _effectiveFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffsetValue => BindConverter.FormatValue(dateTimeOffsetValue, _effectiveFormat, CultureInfo.InvariantCulture),
            DateOnly dateOnlyValue => BindConverter.FormatValue(dateOnlyValue, _effectiveFormat, CultureInfo.InvariantCulture),
            TimeOnly timeOnlyValue => BindConverter.FormatValue(timeOnlyValue, _effectiveFormat, CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        ResolveMetadata();
        _effectiveFormat = string.IsNullOrWhiteSpace(Format) ? _metadata.DefaultFormat : Format;
        base.OnParametersSet();
        CachePickerState();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeType result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result)) {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = $"Failed to parse {value} into a {typeof(DateTimeType).Name}";
        return false;
    }

    private bool IsPickerTriggerDisabled => Disabled ?? Form?.Disabled ?? false;

    private string BuildPickerId() => $"{EffectiveInputId}-picker";

    private void CachePickerState() {
        _pickerId = BuildPickerId();
        _pickerHeadlineId = $"{_pickerId}-headline";
        _pickerClass = $"tnt-dtp-overlay tnt-dtp-mode-{_metadata.PickerMode}";
        _rootClass = BuildRootClass(Density ?? Form?.Density ?? NTFormDensity.Standard);
        _resolvedPickerTriggerIcon = ResolvePickerTriggerIcon();
    }

    private string BuildRootClass(NTFormDensity density) {
        var densityClass = density switch {
            NTFormDensity.Comfortable => "nt-input-date-time-comfortable",
            NTFormDensity.Dense => "nt-input-date-time-dense",
            _ => "nt-input-date-time-standard"
        };

        return EnableCustomPicker ? $"nt-input-date-time nt-input-date-time-custom-picker {densityClass}" : $"nt-input-date-time {densityClass}";
    }

    private void ResolveMetadata() {
        var monthOnly = _underlyingDateTimeType == typeof(DateOnly) && MonthOnly;
        if (_metadataResolved && _metadataMonthOnly == monthOnly) {
            return;
        }

        _metadata = CreateMetadata(monthOnly);
        _metadataMonthOnly = monthOnly;
        _metadataResolved = true;
    }

    private static DateTimeInputMetadata CreateMetadata(bool monthOnly) {
        if (_underlyingDateTimeType == typeof(DateTime) || _underlyingDateTimeType == typeof(DateTimeOffset)) {
            return new DateTimeInputMetadata(InputType.DateTime, "yyyy-MM-ddTHH:mm:ss", "datetime", "Date and time", "Select date and time", "Open date and time picker", MaterialIcon.CalendarMonth);
        }

        if (_underlyingDateTimeType == typeof(TimeOnly)) {
            return new DateTimeInputMetadata(InputType.Time, "HH:mm:ss", "time", "Time", "Select time", "Open time picker", MaterialIcon.Schedule);
        }

        if (_underlyingDateTimeType == typeof(DateOnly)) {
            return monthOnly
                ? new DateTimeInputMetadata(InputType.Month, "yyyy-MM", "month", "Month", "Select month", "Open month picker", MaterialIcon.CalendarMonth)
                : new DateTimeInputMetadata(InputType.Date, "yyyy-MM-dd", "date", "Date", "Select date", "Open date picker", MaterialIcon.CalendarMonth);
        }

        throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.");
    }

    private TnTIcon ResolvePickerTriggerIcon() {
        var pickerIcon = PickerTriggerIcon ?? _metadata.DefaultPickerIcon;
        if (pickerIcon is not MaterialIcon materialIcon) {
            return pickerIcon;
        }

        if (_filledPickerIcon is not null
            && string.Equals(_filledPickerIconName, materialIcon.Icon, StringComparison.Ordinal)
            && EqualityComparer<TnTColor>.Default.Equals(_filledPickerIconColor, materialIcon.Color)
            && _filledPickerIconSize == materialIcon.Size
            && ReferenceEquals(_filledPickerIconTooltip, materialIcon.Tooltip)) {
            return _filledPickerIcon;
        }

        _filledPickerIcon = new MaterialIcon(materialIcon.Icon) {
            Appearance = IconAppearance.Filled,
            Color = materialIcon.Color,
            Size = materialIcon.Size,
            Tooltip = materialIcon.Tooltip
        };
        _filledPickerIconName = materialIcon.Icon;
        _filledPickerIconColor = materialIcon.Color;
        _filledPickerIconSize = materialIcon.Size;
        _filledPickerIconTooltip = materialIcon.Tooltip;
        return _filledPickerIcon;
    }

    private sealed record DateTimeInputMetadata(InputType Type, string DefaultFormat, string PickerMode, string PickerHeadline, string PickerSupportingLabel, string PickerTriggerAriaLabel, TnTIcon DefaultPickerIcon);

}
