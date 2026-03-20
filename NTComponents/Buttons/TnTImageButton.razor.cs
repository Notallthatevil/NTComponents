using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents an image button component with customizable icon and optional badge.
/// </summary>
public partial class TnTImageButton {

    /// <summary>
    ///     Provides an explicit accessible name for the button when it does not contain visible text.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <inheritdoc />
    [Parameter]
    public virtual ButtonAppearance Appearance { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     The badge to be displayed on the button, if any.
    /// </summary>
    [Parameter]
    public TnTBadge? Badge { get; set; }

    /// <summary>
    ///     The size of the button.
    /// </summary>
    [Parameter]
    public Size ButtonSize { get; set; } = Size.Small;

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
        .AddClass("tnt-image-button-round", ImageButtonAppearance == ImageButtonAppearance.Round)
        .AddClass("tnt-image-button-wide", ImageButtonAppearance == ImageButtonAppearance.Wide)
        .AddClass("tnt-image-button-narrow", ImageButtonAppearance == ImageButtonAppearance.Narrow)
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

    /// <summary>
    ///     The icon to be rendered inside the button.
    /// </summary>
    [Parameter, EditorRequired]
    public TnTIcon Icon { get; set; } = default!;

    /// <summary>
    ///     The appearance of the image button, which can be Round, Wide, or Narrow.
    /// </summary>
    [Parameter]
    public ImageButtonAppearance ImageButtonAppearance { get; set; } = ImageButtonAppearance.Round;

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

    internal string? AccessibleLabel =>
        AdditionalAttributes?.ContainsKey("aria-label") == true || AdditionalAttributes?.ContainsKey("aria-labelledby") == true
            ? null
            : AriaLabel ?? ElementTitle;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (Icon == null) {
            throw new ArgumentNullException(nameof(Icon), "TnTImageButton requires a non-null Icon parameter.");
        }
        base.OnParametersSet();
    }
}

/// <summary>
///     Specifies the appearance of the image button.
/// </summary>
public enum ImageButtonAppearance {

    /// <summary>
    ///     The button has a round appearance.
    /// </summary>
    Round,

    /// <summary>
    ///     The button has a wide rectangular appearance.
    /// </summary>
    Wide,

    /// <summary>
    ///     The button has a narrow rectangular appearance.
    /// </summary>
    Narrow
}
