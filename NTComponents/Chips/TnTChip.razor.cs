using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Represents a chip component with various customizable properties and events.
/// </summary>
public partial class TnTChip
{

    /// <inheritdoc />
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SecondaryContainer;

    /// <summary>
    ///     The event callback to be invoked after the value is bound.
    /// </summary>
    [Parameter]
    public EventCallback<bool> BindAfter { get; set; }

    /// <summary>
    ///     The event callback to be invoked when the close button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> CloseButtonClicked { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the toggle functionality is disabled.
    /// </summary>
    [Parameter]
    public bool DisableToggle { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-chip")
        .AddDisabled(Disabled)
        .AddClass("tnt-interactable", !DisableToggle)
        .AddRipple(EnableRipple)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string ElementName { get; set; } = TnTComponentIdentifier.NewId();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-chip-background-color", BackgroundColor)
        .AddVariable("tnt-chip-text-color", TextColor)
        .AddVariable("tnt-chip-on-tint-color", OnTintColor)
        .AddVariable("tnt-chip-tint-color", TintColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     The label of the chip.
    /// </summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public TnTColor OnTintColor { get; set; } = TnTColor.OnPrimary;

    /// <summary>
    ///     Gets or sets a value indicating whether the chip is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     The start icon of the chip.
    /// </summary>
    [Parameter]
    public TnTIcon? StartIcon { get; set; }

    /// <summary>
    ///     Optional secondary text displayed below the label. Rendered in a smaller font within the same chip height.
    /// </summary>
    [Parameter]
    public string? SubLabel { get; set; }

    /// <summary>
    ///     The text color of the chip.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSecondaryContainer;

    /// <inheritdoc />
    [Parameter]
    public TnTColor TintColor { get; set; } = TnTColor.SurfaceTint;

    /// <summary>
    ///     The value of the chip.
    /// </summary>
    [Parameter]
    public bool Value { get; set; }

    /// <summary>
    ///     The event callback to be invoked when the value changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    private async Task OnChangeAsync(ChangeEventArgs e) {
        if (e.Value is bool value) {
            Value = value;
            await ValueChanged.InvokeAsync(value);
            await BindAfter.InvokeAsync(value);
        }
    }

}