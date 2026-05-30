using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned numeric input.
/// </summary>
/// <remarks>
///     Use <see cref="NTInputNumeric{TNumber}" /> for single-line numeric entry. Add <c>min</c>, <c>max</c>, or
///     <see cref="System.ComponentModel.DataAnnotations.RangeAttribute" /> when the value has a known range, and use
///     prefix or suffix text for short fixed context such as currency symbols, units, or scales.
/// </remarks>
/// <remarks>
///     Set <typeparamref name="TNumber" /> to the non-nullable numeric type, such as <c>int</c>, <c>decimal</c>, or
///     <see cref="BigInteger" />. The component value is nullable, so model properties such as <c>int?</c> and
///     <c>decimal?</c> are supported while unsupported generic types fail at compile time.
/// </remarks>
/// <typeparam name="TNumber">The non-nullable numeric value type.</typeparam>
public class NTInputNumeric<TNumber> : NTInputBase<TNumber?> where TNumber : struct, INumber<TNumber> {

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => InputType.Number;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TNumber? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (string.IsNullOrWhiteSpace(value)) {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (TNumber.TryParse(value, CultureInfo.InvariantCulture, out var parsedValue)) {
            result = parsedValue;
            validationErrorMessage = null;
            return true;
        }

        result = null;
        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, "The {0} field must be a number.", DisplayName ?? FieldIdentifier.FieldName);
        return false;
    }
}
