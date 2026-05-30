using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned text input.
/// </summary>
/// <remarks>
///     Use <see cref="NTInputText" /> for single-line text entry. Choose the most specific <see cref="InputType" /> value
///     so browsers can provide the right keyboard, autofill, validation hints, and built-in behavior for the requested data.
/// </remarks>
public partial class NTInputText {

    /// <summary>
    ///     Gets or sets the native text input type.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="TextInputType.Text" /> only for general free text. Prefer <see cref="TextInputType.Email" />,
    ///     <see cref="TextInputType.Tel" />, <see cref="TextInputType.Url" />, or <see cref="TextInputType.Search" /> when
    ///     the field has a more specific purpose.
    /// </remarks>
    [Parameter]
    public TextInputType InputType { get; set; } = TextInputType.Text;

    /// <summary>
    ///     Gets or sets the phone formatting mask used when <see cref="InputType" /> is <see cref="TextInputType.Tel" />.
    /// </summary>
    /// <remarks>
    ///     Use <c>#</c> for digit placeholders and literal characters for country or local formatting, for example
    ///     <c>(###) ###-####</c>, <c>+## #### ######</c>, or <c>+## ## ## ## ##</c>. The mask is ignored for non-tel text
    ///     input types.
    /// </remarks>
    [Parameter]
    public string PhoneMask { get; set; } = "(###) ###-####";

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => InputType switch {
        TextInputType.Text => NTComponents.InputType.Text,
        TextInputType.Email => NTComponents.InputType.Email,
        TextInputType.Password => NTComponents.InputType.Password,
        TextInputType.Tel => NTComponents.InputType.Tel,
        TextInputType.Url => NTComponents.InputType.Url,
        TextInputType.Search => NTComponents.InputType.Search,
        _ => throw new InvalidOperationException($"{InputType} is not a valid value for {nameof(TextInputType)}")
    };

    /// <inheritdoc />
    protected override string? NativeOnInputHandler => ShouldApplyPhoneMask ? "window.NTComponents?.applyPhoneMaskInput?.(this)" : null;

    private bool ShouldApplyPhoneMask => InputType == TextInputType.Tel && !string.IsNullOrWhiteSpace(PhoneMask) && PhoneMask.Contains('#', StringComparison.Ordinal);

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?>? BuildAdditionalInputAttributes() {
        if (!ShouldApplyPhoneMask) {
            return null;
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["phoneMask"] = PhoneMask
        };
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(string? value) => ShouldApplyPhoneMask ? ApplyPhoneMask(value, PhoneMask) : value;

    /// <inheritdoc />
    protected override string? FormatFormPostValue() => ShouldApplyPhoneMask ? NormalizePhoneValue(CurrentValue, PhoneMask) : base.FormatFormPostValue();

    /// <inheritdoc />
    protected override async Task OnInputAsync(string? newValue) => await base.OnInputAsync(ShouldApplyPhoneMask ? NormalizePhoneValue(newValue, PhoneMask) : newValue);

    /// <inheritdoc />
    protected override bool UsesSeparateFormPostValue => ShouldApplyPhoneMask;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = ShouldApplyPhoneMask ? NormalizePhoneValue(value, PhoneMask) : value;
        validationErrorMessage = null;
        return true;
    }

    private static string? ApplyPhoneMask(string? value, string mask) {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(mask) || !mask.Contains('#', StringComparison.Ordinal)) {
            return value;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) {
            return string.Empty;
        }

        var maximumDigits = mask.Count(character => character == '#');
        if (digits.Length > maximumDigits) {
            digits = digits[..maximumDigits];
        }

        var builder = new StringBuilder(mask.Length);
        var digitIndex = 0;
        foreach (var character in mask) {
            if (character == '#') {
                if (digitIndex >= digits.Length) {
                    break;
                }

                builder.Append(digits[digitIndex]);
                digitIndex++;
                continue;
            }

            if (digitIndex >= digits.Length && builder.Length > 0) {
                break;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static int GetPhoneCountryCodeDigitCount(string mask) {
        var trimmedMask = mask.TrimStart();
        if (!trimmedMask.StartsWith('+')) {
            return 0;
        }

        var digitCount = 0;
        foreach (var character in trimmedMask[1..]) {
            if (character != '#') {
                break;
            }

            digitCount++;
        }

        return digitCount;
    }

    private static string? NormalizePhoneValue(string? value, string mask) {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(mask) || !mask.Contains('#', StringComparison.Ordinal)) {
            return value;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) {
            return string.Empty;
        }

        var maximumDigits = mask.Count(character => character == '#');
        if (digits.Length > maximumDigits) {
            digits = digits[..maximumDigits];
        }

        var countryCodeDigitCount = GetPhoneCountryCodeDigitCount(mask);
        return countryCodeDigitCount > 0 && digits.Length > countryCodeDigitCount
            ? $"{digits[..countryCodeDigitCount]} {digits[countryCodeDigitCount..]}"
            : digits;
    }
}
