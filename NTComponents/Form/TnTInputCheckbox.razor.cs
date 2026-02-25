using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Represents a checkbox input component.
/// </summary>
public partial class TnTInputCheckbox {

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create(base.ElementClass ?? string.Empty)
        .AddClass("checkbox-appearance-default", Layout is CheckboxLayout.Default)
        .AddClass("checkbox-appearance-span-and-flip", Layout is CheckboxLayout.FlipAndSpan)
        .Build();

    /// <summary>
    ///     Gets or sets the layout style used to render the checkbox component.
    /// </summary>
    /// <remarks>Use this property to control the visual arrangement and presentation of the checkbox. The default layout is applied if no value is specified.</remarks>
    [Parameter]
    public CheckboxLayout Layout { get; set; } = CheckboxLayout.Default;

    /// <inheritdoc />
    public override InputType Type => InputType.Checkbox;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? validationErrorMessage) => throw new NotSupportedException();
}