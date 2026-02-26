using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     A custom input component for handling various DateTime types.
/// </summary>
/// <typeparam name="DateTimeType">The type of the DateTime value.</typeparam>
public partial class TnTInputDateTime<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] DateTimeType> : ITnTPageScriptComponent<TnTInputDateTime<DateTimeType>> {

    /// <summary>
    ///     Gets or sets a value indicating whether the Material picker is enabled.
    /// </summary>
    [Parameter]
    public bool EnableMaterialPicker { get; set; } = true;

    /// <summary>
    ///     Gets or sets the format string used to display the DateTime value.
    /// </summary>
    [Parameter]
    public string Format { get; set; } = default!;

    /// <summary>
    ///     Gets or sets a value indicating whether to display only the month part of the DateTime value.
    /// </summary>
    [Parameter]
    public bool MonthOnly { get; set; }

    /// <summary>
    ///     Gets or sets date values that should be disabled in the picker.
    /// </summary>
    [Parameter]
    public IEnumerable<DateOnly>? DisabledDates { get; set; }

    /// <summary>
    ///     Gets or sets time values that should be disabled in the picker.
    /// </summary>
    [Parameter]
    public IEnumerable<TimeOnly>? DisabledTimes { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the picker opens when the input receives focus.
    /// </summary>
    [Parameter]
    public bool OpenPickerOnFocus { get; set; } = true;

    /// <summary>
    ///     Gets or sets optional custom action buttons for the picker.
    /// </summary>
    [Parameter]
    public RenderFragment? PickerActionButtons { get; set; }

    /// <summary>
    ///     Gets or sets additional CSS classes to apply to the picker cancel button.
    /// </summary>
    [Parameter]
    public string? PickerCancelButtonClass { get; set; }

    /// <summary>
    ///     Gets or sets the text used for the picker cancel button.
    /// </summary>
    [Parameter]
    public string PickerCancelButtonText { get; set; } = "Cancel";

    /// <summary>
    ///     Gets or sets additional CSS classes to apply to the picker clear button.
    /// </summary>
    [Parameter]
    public string? PickerClearButtonClass { get; set; }

    /// <summary>
    ///     Gets or sets the text used for the picker clear button.
    /// </summary>
    [Parameter]
    public string PickerClearButtonText { get; set; } = "Clear";

    /// <summary>
    ///     Gets or sets additional CSS classes to apply to the picker confirm button.
    /// </summary>
    [Parameter]
    public string? PickerConfirmButtonClass { get; set; }

    /// <summary>
    ///     Gets or sets the text used for the picker confirm button.
    /// </summary>
    [Parameter]
    public string PickerConfirmButtonText { get; set; } = "OK";

    /// <summary>
    ///     Gets or sets additional CSS classes to apply to the picker quick action buttons.
    /// </summary>
    [Parameter]
    public string? PickerQuickActionButtonClass { get; set; }

    /// <summary>
    ///     Gets or sets the text used for the picker "Now" action button.
    /// </summary>
    [Parameter]
    public string PickerNowButtonText { get; set; } = "Now";

    /// <summary>
    ///     Gets or sets the text used for the picker "Today" action button.
    /// </summary>
    [Parameter]
    public string PickerTodayButtonText { get; set; } = "Today";

    /// <summary>
    ///     Gets or sets a value indicating whether the picker clear button is rendered.
    /// </summary>
    [Parameter]
    public bool ShowPickerClearButton { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the picker "Now" quick action button is rendered.
    /// </summary>
    [Parameter]
    public bool ShowPickerNowButton { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the picker "Today" quick action button is rendered.
    /// </summary>
    [Parameter]
    public bool ShowPickerTodayButton { get; set; } = true;

    /// <inheritdoc />
    public override InputType Type => _type;

    /// <summary>
    ///     Gets the CSS class applied to the default cancel button.
    /// </summary>
    protected string PickerCancelButtonCssClass => BuildPickerActionClass("tnt-dtp-action-button tnt-dtp-text-button", PickerCancelButtonClass);

    /// <summary>
    ///     Gets the CSS class applied to the default clear button.
    /// </summary>
    protected string PickerClearButtonCssClass => BuildPickerActionClass("tnt-dtp-action-button tnt-dtp-tonal-button", PickerClearButtonClass ?? PickerQuickActionButtonClass);

    /// <summary>
    ///     Gets the CSS class applied to the default confirm button.
    /// </summary>
    protected string PickerConfirmButtonCssClass => BuildPickerActionClass("tnt-dtp-action-button tnt-dtp-filled-button", PickerConfirmButtonClass);

    /// <summary>
    ///     Gets the CSS class applied to the default "Now" quick action button.
    /// </summary>
    protected string PickerNowButtonCssClass => BuildPickerActionClass("tnt-dtp-action-button tnt-dtp-tonal-button", PickerQuickActionButtonClass);

    /// <summary>
    ///     Gets the mode value emitted as a data attribute for JS interop.
    /// </summary>
    protected string PickerModeValue => _pickerMode switch {
        DateTimePickerMode.Date => "date",
        DateTimePickerMode.Month => "month",
        DateTimePickerMode.Time => "time",
        DateTimePickerMode.DateTime => "datetime",
        _ => "none"
    };

    /// <summary>
    ///     Gets the element id used by the picker popup.
    /// </summary>
    protected string PickerPopupId { get; } = $"tnt-dtp-{Guid.NewGuid():N}";

    /// <summary>
    ///     Gets the reference to the DotNet object associated with the component.
    /// </summary>
    public DotNetObjectReference<TnTInputDateTime<DateTimeType>>? DotNetObjectRef { get; private set; }

    /// <summary>
    ///     Gets the reference to the isolated JavaScript module.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <summary>
    ///     Gets the path of the JavaScript module.
    /// </summary>
    public string? JsModulePath => "./_content/NTComponents/Form/TnTInputDateTime.razor.js";

    /// <summary>
    ///     Gets a value indicating whether a date panel should be rendered in the picker.
    /// </summary>
    protected bool ShowDatePickerPanel => _pickerMode is DateTimePickerMode.Date or DateTimePickerMode.DateTime;

    /// <summary>
    ///     Gets a value indicating whether a time panel should be rendered in the picker.
    /// </summary>
    protected bool ShowTimePickerPanel => _pickerMode is DateTimePickerMode.Time or DateTimePickerMode.DateTime;

    /// <summary>
    ///     Gets a value indicating whether a month panel should be rendered in the picker.
    /// </summary>
    protected bool ShowMonthPickerPanel => _pickerMode is DateTimePickerMode.Month;

    /// <summary>
    ///     Gets disabled date values encoded for JS interop.
    /// </summary>
    protected string? DisabledDateValuesAttribute => BuildDisabledDateValuesAttribute();

    /// <summary>
    ///     Gets disabled time values encoded for JS interop.
    /// </summary>
    protected string? DisabledTimeValuesAttribute => BuildDisabledTimeValuesAttribute();

    /// <summary>
    ///     Gets a value indicating whether the Material picker is active for the current input type.
    /// </summary>
    protected bool UseMaterialPicker => EnableMaterialPicker && _pickerMode is not DateTimePickerMode.None;

    /// <summary>
    ///     Gets a value indicating whether the picker "Now" quick action is rendered.
    /// </summary>
    protected bool UseNowQuickAction => (_pickerMode is DateTimePickerMode.Time or DateTimePickerMode.DateTime or DateTimePickerMode.Month) && ShowPickerNowButton;

    /// <summary>
    ///     Gets a value indicating whether the picker "Today" quick action is rendered.
    /// </summary>
    protected bool UseTodayQuickAction => (_pickerMode is DateTimePickerMode.Date) && ShowPickerTodayButton;

    /// <summary>
    ///     Gets the CSS class applied to the default "Today" quick action button.
    /// </summary>
    protected string PickerTodayButtonCssClass => BuildPickerActionClass("tnt-dtp-action-button tnt-dtp-tonal-button", PickerQuickActionButtonClass);

    private string _format = default!;
    private DateTimePickerMode _pickerMode;
    private InputType _type;

    /// <summary>
    ///     Gets the JavaScript runtime for interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTInputDateTime{DateTimeType}" /> class.
    /// </summary>
    public TnTInputDateTime() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <inheritdoc />
    protected override string? FormatValueAsString(DateTimeType? value) {
        var result = value switch {
            DateTime dateTimeValue => BindConverter.FormatValue(dateTimeValue, _format, CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffsetValue => BindConverter.FormatValue(dateTimeOffsetValue, _format, CultureInfo.InvariantCulture),
            DateOnly dateOnlyValue => BindConverter.FormatValue(dateOnlyValue, _format, CultureInfo.InvariantCulture),
            TimeOnly timeOnlyValue => BindConverter.FormatValue(timeOnlyValue, _format, CultureInfo.InvariantCulture),
            _ => string.Empty, // Handles null for Nullable<DateTime>, etc.
        };

        return result;
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        ResolveMetadataFromType();
        ApplyFormatAttribute();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        try {
            if (firstRender) {
                IsolatedJsModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                await (IsolatedJsModule?.InvokeVoidAsync("onLoad", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (IsolatedJsModule?.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ResolveMetadataFromType();
        if (string.IsNullOrWhiteSpace(Format)) {
            Format = _format;
        }
        ApplyFormatAttribute();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeType result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result)) {
            validationErrorMessage = null;
            return true;
        }
        else {
            validationErrorMessage = $"Failed to parse {value} into a {typeof(DateTimeType).Name}";
            return false;
        }
    }

    private static string BuildPickerActionClass(string defaultClass, string? customClass) {
        if (string.IsNullOrWhiteSpace(customClass)) {
            return defaultClass;
        }

        return $"{defaultClass} {customClass}";
    }

    private string? BuildDisabledDateValuesAttribute() {
        if (DisabledDates is null) {
            return null;
        }

        var values = DisabledDates
            .Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return values.Length == 0 ? null : string.Join(',', values);
    }

    private string? BuildDisabledTimeValuesAttribute() {
        if (DisabledTimes is null) {
            return null;
        }

        var values = DisabledTimes
            .Select(time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return values.Length == 0 ? null : string.Join(',', values);
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the component and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
            // Do not dispose IsolatedJsModule here; it should be disposed asynchronously in DisposeAsyncCore.
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the component asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }
            IsolatedJsModule = null;
        }

        if (DotNetObjectRef is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else {
            DotNetObjectRef?.Dispose();
        }
        DotNetObjectRef = null;
    }

    private void ApplyFormatAttribute() {
        _format = (Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType)) switch {
            var t when t == typeof(DateTime) => "yyyy-MM-ddTHH:mm:ss",
            var t when t == typeof(DateTimeOffset) => "yyyy-MM-ddTHH:mm:ss",
            var t when t == typeof(TimeOnly) => "HH:mm:ss",
            var t when t == typeof(DateOnly) => MonthOnly ? "yyyy-MM" : "yyyy-MM-dd",
            _ => throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.")
        };

        var attributes = AdditionalAttributes is null ? [] : new Dictionary<string, object>(AdditionalAttributes);
        attributes["format"] = _format;
        AdditionalAttributes = attributes;
    }

    private void ResolveMetadataFromType() {
        var targetType = Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType);

        _type = targetType switch {
            var t when t == typeof(DateTime) => InputType.DateTime,
            var t when t == typeof(DateTimeOffset) => InputType.DateTime,
            var t when t == typeof(TimeOnly) => InputType.Time,
            var t when t == typeof(DateOnly) => MonthOnly ? InputType.Month : InputType.Date,
            _ => throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.")
        };

        _pickerMode = targetType switch {
            var t when t == typeof(DateOnly) && !MonthOnly => DateTimePickerMode.Date,
            var t when t == typeof(DateOnly) && MonthOnly => DateTimePickerMode.Month,
            var t when t == typeof(TimeOnly) => DateTimePickerMode.Time,
            var t when t == typeof(DateTime) || t == typeof(DateTimeOffset) => DateTimePickerMode.DateTime,
            _ => DateTimePickerMode.None
        };
    }

    private enum DateTimePickerMode {
        None = 0,
        Date,
        Month,
        Time,
        DateTime
    }
}
