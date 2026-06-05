#pragma warning disable CS0618
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using NTComponents.Ext;

namespace NTComponents;

/// <summary>
///     Represents a currency input component.
/// </summary>
public partial class TnTInputCurrency : TnTInputBase<decimal?> {

    /// <summary>
    ///     Gets or sets the culture code for formatting the currency.
    /// </summary>
    [Parameter]
    public string CultureCode { get; set; } = "en-US";

    /// <summary>
    ///     Gets or sets the currency code.
    /// </summary>
    [Parameter]
    public string CurrencyCode { get; set; } = "USD";

    /// <inheritdoc />
    public override InputType Type => InputType.Currency;

    private CultureInfo _cultureInfo = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _cultureInfo = CultureInfo.GetCultureInfo(CultureCode);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out decimal? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        validationErrorMessage = null;
        if (value is not null) {
            if (decimal.TryParse(value?.TrimStart('$'), NumberStyles.Currency, _cultureInfo, out var r)) {
                result = r;
                return true;
            }
            else if (value is null) {
                result = null;
                return true;
            }
            else {
                result = null;
                validationErrorMessage = $"Failed to parse {value} into a {typeof(decimal).Name}";
                return false;
            }
        }
        else {
            result = null;
            validationErrorMessage = null;
            return true;
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(decimal? value) => value?.ToString("C", _cultureInfo);
}