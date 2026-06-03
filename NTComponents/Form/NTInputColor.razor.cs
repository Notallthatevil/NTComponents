using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     A Material 3 aligned color input.
/// </summary>
/// <remarks>
///     Use <see cref="NTInputColor" /> when a field should render the browser's native color picker while keeping the
///     Form text-field container, validation, density, and appearance behavior.
/// </remarks>
public partial class NTInputColor {

    /// <inheritdoc />
    protected override InputType InputTypeAttribute => InputType.Color;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
