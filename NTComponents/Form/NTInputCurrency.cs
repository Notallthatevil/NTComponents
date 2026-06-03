using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned currency input.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NTInputCurrency" /> is a specialized <see cref="NTInputNumeric{TNumber}" /> for nullable
///         <see cref="decimal" /> values. It renders as a text input so currency symbols and grouping separators can be
///         displayed after blur while preserving the Form text-field layout, validation, supporting text, and icon
///         behavior. While users are editing, the field only keeps the currency symbol prefix and preserves the typed
///         digits, separators, and decimal point.
///     </para>
///     <para>
///         Use <see cref="CultureCode" /> and <see cref="CurrencyCode" /> together so the displayed symbol, separators, and
///         currency precision match the expected locale.
///     </para>
/// </remarks>
public class NTInputCurrency : NTInputNumeric<decimal> {
    private CultureInfo _cultureInfo = CultureInfo.GetCultureInfo("en-US");
    private CultureInfo _formatCultureInfo = CultureInfo.GetCultureInfo("en-US");
    private string _currencySymbol = "$";
    private int _currencyDecimalDigits = 2;
    private bool _isEditing;
    private string? _editingText;

    /// <summary>
    ///     Gets or sets the culture code used for parsing and formatting the currency value.
    /// </summary>
    [Parameter]
    public string CultureCode { get; set; } = "en-US";

    /// <summary>
    ///     Gets or sets the ISO 4217 currency code used by the native currency formatter.
    /// </summary>
    [Parameter]
    public string CurrencyCode { get; set; } = "USD";

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => InputType.Text;

    /// <inheritdoc />
    protected override string? NativeOnInputHandler => "window.NTComponents?.prefixCurrencyInput?.(this)";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["cultureCode"] = CultureCode,
            ["currencyCode"] = CurrencyCode,
            ["currencyDecimalDigits"] = _currencyDecimalDigits.ToString(CultureInfo.InvariantCulture),
            ["currencyDecimalSeparator"] = _formatCultureInfo.NumberFormat.CurrencyDecimalSeparator,
            ["currencyGroupSeparator"] = _formatCultureInfo.NumberFormat.CurrencyGroupSeparator,
            ["currencySymbol"] = _currencySymbol,
            ["inputmode"] = "decimal"
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(decimal? value) => _isEditing ? _editingText : value?.ToString("C", _formatCultureInfo);

    /// <inheritdoc />
    protected override string? FormatFormPostValue() => CurrentValue?.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override async Task OnBlurAsync(FocusEventArgs args) {
        _isEditing = false;
        _editingText = null;

        await base.OnBlurAsync(args);
    }

    /// <inheritdoc />
    protected override async Task OnInputAsync(string? newValue) {
        _isEditing = true;
        _editingText = FormatEditingValue(newValue);

        await base.OnInputAsync(_editingText);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _cultureInfo = CultureInfo.GetCultureInfo(CultureCode);
        _currencySymbol = ResolveCurrencySymbol(_cultureInfo, CurrencyCode);
        _formatCultureInfo = CreateCurrencyFormatCulture(_cultureInfo, _currencySymbol);
        _currencyDecimalDigits = _formatCultureInfo.NumberFormat.CurrencyDecimalDigits;

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override bool UsesSeparateFormPostValue => true;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out decimal? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (string.IsNullOrWhiteSpace(value) || IsCurrencySymbolOnly(value)) {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        var normalizedValue = LimitFractionalDigits(value);
        if (decimal.TryParse(normalizedValue, NumberStyles.Currency, _formatCultureInfo, out var parsedValue)) {
            result = parsedValue;
            validationErrorMessage = null;
            return true;
        }

        result = null;
        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, "The {0} field must be a currency amount.", DisplayName ?? FieldIdentifier.FieldName);
        return false;
    }

    private string? FormatEditingValue(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return value;
        }

        if (string.IsNullOrEmpty(_currencySymbol) || value.StartsWith(_currencySymbol, StringComparison.Ordinal)) {
            return LimitFractionalDigits(value);
        }

        return LimitFractionalDigits($"{_currencySymbol}{value}");
    }

    private static CultureInfo CreateCurrencyFormatCulture(CultureInfo cultureInfo, string currencySymbol) {
        var culture = (CultureInfo)cultureInfo.Clone();
        culture.NumberFormat.CurrencySymbol = currencySymbol;
        return culture;
    }

    private bool IsCurrencySymbolOnly(string value) {
        var trimmedValue = value.Trim();
        if (trimmedValue.Length == 0) {
            return true;
        }

        return trimmedValue.Equals(_currencySymbol, StringComparison.Ordinal)
            || trimmedValue.Equals(CurrencyCode, StringComparison.OrdinalIgnoreCase);
    }

    private string LimitFractionalDigits(string value) {
        var decimalSeparator = _formatCultureInfo.NumberFormat.CurrencyDecimalSeparator;
        if (string.IsNullOrEmpty(decimalSeparator)) {
            return value;
        }

        var decimalSeparatorIndex = value.IndexOf(decimalSeparator, StringComparison.Ordinal);
        if (decimalSeparatorIndex < 0) {
            return value;
        }

        if (_currencyDecimalDigits <= 0) {
            return value[..decimalSeparatorIndex];
        }

        var fractionalStartIndex = decimalSeparatorIndex + decimalSeparator.Length;
        var maximumLength = fractionalStartIndex + _currencyDecimalDigits;
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static string ResolveCurrencySymbol(CultureInfo cultureInfo, string currencyCode) {
        if (string.IsNullOrWhiteSpace(currencyCode)) {
            return cultureInfo.NumberFormat.CurrencySymbol;
        }

        if (!cultureInfo.IsNeutralCulture && TryGetCurrencySymbol(cultureInfo.Name, currencyCode, out var symbol)) {
            return symbol;
        }

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures)) {
            if (TryGetCurrencySymbol(culture.Name, currencyCode, out symbol)) {
                return symbol;
            }
        }

        return currencyCode;
    }

    private static bool TryGetCurrencySymbol(string cultureName, string currencyCode, [NotNullWhen(true)] out string? currencySymbol) {
        try {
            var region = new RegionInfo(cultureName);
            if (region.ISOCurrencySymbol.Equals(currencyCode, StringComparison.OrdinalIgnoreCase)) {
                currencySymbol = region.CurrencySymbol;
                return true;
            }
        }
        catch (ArgumentException) {
        }

        currencySymbol = null;
        return false;
    }
}
