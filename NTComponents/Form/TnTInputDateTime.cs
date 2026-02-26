using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NTComponents;

/// <summary>
///     A custom input component for handling various DateTime types.
/// </summary>
/// <typeparam name="DateTimeType">The type of the DateTime value.</typeparam>
public class TnTInputDateTime<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] DateTimeType> : TnTInputBase<DateTimeType> {

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

    /// <inheritdoc />
    public override InputType Type => _type;

    private string _format = default!;
    private InputType _type;

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

        _format = (Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType)) switch {
            var t when t == typeof(DateTime) => "yyyy-MM-ddTHH:mm:ss",
            var t when t == typeof(DateTimeOffset) => "yyyy-MM-ddTHH:mm:ss",
            var t when t == typeof(TimeOnly) => "HH:mm:ss",
            var t when t == typeof(DateOnly) => MonthOnly ? "yyyy-MM" : "yyyy-MM-dd",
            _ => throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.")
        };

        _type = (Nullable.GetUnderlyingType(typeof(DateTimeType)) ?? typeof(DateTimeType)) switch {
            var t when t == typeof(DateTime) => InputType.DateTime,
            var t when t == typeof(DateTimeOffset) => InputType.DateTime,
            var t when t == typeof(TimeOnly) => InputType.Time,
            var t when t == typeof(DateOnly) => MonthOnly ? InputType.Month : InputType.Date,
            _ => throw new InvalidOperationException($"The type '{typeof(DateTimeType)}' is not a supported DateTime type.")
        };

        var attributes = AdditionalAttributes is null ? [] : new Dictionary<string, object>(AdditionalAttributes);
        attributes.Add("format", _format);
        AdditionalAttributes = attributes;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Format)) {
            Format = _format;
        }
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
}