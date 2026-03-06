using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Reflection.Metadata;
using System.Xml.Linq;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Represents a customizable button component.
/// </summary>
public partial class TnTButton : TnTComponentBase {

    /// <inheritdoc />
    [Parameter]
    public virtual ButtonAppearance Appearance { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     The size of the button.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    ///     The content to be rendered inside the button.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddTextAlign(TextAlignment)
        .AddClass("tnt-filled", Appearance is ButtonAppearance.Filled or ButtonAppearance.Elevated)
        .AddClass("tnt-outlined", Appearance == ButtonAppearance.Outlined)
        .AddClass("tnt-text", Appearance == ButtonAppearance.Text)
        .AddClass("tnt-elevated", Appearance == ButtonAppearance.Elevated)
        .AddClass("tnt-button-square", Shape == ButtonShape.Square)
        .AddClass("tnt-button-tint-color", TintColor.HasValue)
        .AddClass("tnt-button-on-tint-color", OnTintColor.HasValue)
        .AddSize(ButtonSize)
        .AddDisabled(Disabled)
        .AddClass("tnt-interactable")
        .AddRipple(EnableRipple)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-button-bg-color", BackgroundColor)
        .AddVariable("tnt-button-fg-color", TextColor)
        .AddVariable("tnt-button-tint-color", TintColor.GetValueOrDefault(), TintColor.HasValue)
        .AddVariable("tnt-button-on-tint-color", OnTintColor.GetValueOrDefault(), OnTintColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public EventCallback<MouseEventArgs> OnClickCallback { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <summary>
    ///     The shape of the button, which can be rounded or square
    /// </summary>
    [Parameter]
    public ButtonShape Shape { get; set; } = ButtonShape.Round;

    /// <summary>
    ///     When set, the click event will not propagate to parent elements.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <inheritdoc />
    [Parameter]
    public virtual TnTColor TextColor { get; set; } = TnTColor.OnPrimary;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; } = TnTColor.SurfaceTint;

    /// <summary>
    ///     The content to display as a tooltip for the component.
    /// </summary>
    [Parameter]
    public RenderFragment Tooltip { get; set; } = default!;

    /// <summary>
    ///     The type of the button.
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; }
}