using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Represents a card component with customizable appearance, background color, border radius, elevation, text alignment, and text color.
/// </summary>
[Obsolete("Use NTCard instead.")]
public partial class TnTCard {

    /// <summary>
    /// The appearance of the card.
    /// </summary>
    [Parameter]
    public CardAppearance Appearance { get; set; } = CardAppearance.Elevated;

    /// <summary>
    ///     The child content to be rendered inside the card.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-card")
        .AddClass("tnt-card-filled", Appearance == CardAppearance.Filled)
        .AddClass("tnt-card-outlined", Appearance == CardAppearance.Outlined)
        .AddClass("tnt-card-elevated", Appearance == CardAppearance.Elevated)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-card-background-color", BackgroundColor)
        .AddVariable("tnt-card-text-color", TextColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public int Elevation { get; set; } = 1;

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    /// The background color of the card.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerLow;

}

/// <summary>
///     Specifies the appearance of a card.
/// </summary>
public enum CardAppearance {

    /// <summary>
    ///     The card is filled.
    /// </summary>
    Filled,

    /// <summary>
    ///     The card is outlined.
    /// </summary>
    Outlined,

    /// <summary>
    ///     The card is elevated.
    /// </summary>
    Elevated
}
