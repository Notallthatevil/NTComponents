#pragma warning disable CS0618
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents;

/// <summary>
///     Represents a numeric input component.
/// </summary>
/// <typeparam name="TNumericType">The type of the numeric value.</typeparam>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public class TnTInputNumeric< [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TNumericType> : TnTInputBase<TNumericType> {

    /// <inheritdoc />
    public override InputType Type => InputType.Number;

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        var type = Nullable.GetUnderlyingType(typeof(TNumericType)) ?? typeof(TNumericType);
        if (type != typeof(int) && type != typeof(long) && type != typeof(short) && type != typeof(float) && type != typeof(double) && type != typeof(decimal)) {
            throw new InvalidOperationException($"The type '{type}' is not a supported numeric type.");
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TNumericType result, [NotNullWhen(false)] out string? validationErrorMessage) {
        if (BindConverter.TryConvertTo<TNumericType>(value, CultureInfo.InvariantCulture, out result)) {
            validationErrorMessage = null;
            return true;
        }

        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, "The {0} field must be a number.", base.DisplayName ?? base.FieldIdentifier.FieldName);
        return false;
    }
}