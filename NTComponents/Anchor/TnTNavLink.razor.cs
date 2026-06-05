using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using System.Diagnostics;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Represents a navigation link (anchor) component with customizable styles and behaviors.
/// </summary>
[Obsolete("Use NTNavLink instead.")]
public partial class TnTNavLink {
    /// <inheritdoc />
    [Parameter]
    public  TnTColor? ActiveBackgroundColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public  TnTColor? ActiveTextColor { get; set; }

    /// <summary>
    ///     The appearance of the anchor.
    /// </summary>
    [Parameter]
    public AnchorAppearance Appearance { get; set; }

    /// <summary>
    /// The size of the anchor, only applies if <see cref="Appearance"/> is <see cref="AnchorAppearance.Filled"/> or <see cref="AnchorAppearance.Outlined"/>.
    /// </summary>
    [Parameter]
    public Size AnchorSize { get; set; } = Size.Small;

    /// <inheritdoc />
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <inheritdoc />
    [Parameter]
    public  TnTColor BackgroundColor { get; set; } = TnTColor.Primary;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    public  string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass(CssClass)
        .AddClass("tnt-nav-link")
        .AddClass("tnt-filled", Appearance == AnchorAppearance.Filled)
        .AddClass("tnt-outlined", Appearance == AnchorAppearance.Outlined)
        .AddClass("tnt-underlined", Appearance == AnchorAppearance.Underlined)
        .AddClass("tnt-anchor-square", Shape == AnchorShape.Square)
        .AddClass("tnt-anchor-tint-color", TintColor.HasValue && (Appearance is AnchorAppearance.Filled or AnchorAppearance.Outlined))
        .AddClass("tnt-anchor-on-tint-color", OnTintColor.HasValue)
        .AddDisabled(Disabled)
        .AddClass("tnt-interactable", Appearance is AnchorAppearance.Filled or AnchorAppearance.Outlined)
        .AddRipple(EnableRipple)
        .AddSize(AnchorSize)
        .AddClass("active-fg-color", ActiveTextColor.HasValue)
        .AddClass("active-bg-color", ActiveBackgroundColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementId { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementLang { get; set; }

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-anchor-active-bg-color", ActiveBackgroundColor.GetValueOrDefault(), ActiveBackgroundColor.HasValue)
        .AddVariable("tnt-anchor-active-fg-color", ActiveTextColor.GetValueOrDefault(), ActiveTextColor.HasValue)
        .AddVariable("tnt-anchor-bg-color", BackgroundColor)
        .AddVariable("tnt-anchor-fg-color", TextColor)
        .AddVariable("tnt-anchor-tint-color", TintColor.GetValueOrDefault(), TintColor.HasValue)
        .AddVariable("tnt-anchor-on-tint-color", OnTintColor.GetValueOrDefault(), OnTintColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <inheritdoc />
    [Parameter]
    public  TnTColor TextColor { get; set; } = TnTColor.OnBackground;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; } = TnTColor.SurfaceTint;

    /// <summary>
    /// The shape of the anchor, which can be rounded or square. Only applies if appearance is set to Filled or Outlined.
    /// </summary>
    [Parameter]
    public AnchorShape Shape { get; set; } = AnchorShape.Round;

    // Render logic moved to TnTNavLink.razor

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        if (Disabled && AdditionalAttributes?.ContainsKey("href") == true) {
            var attributes = new Dictionary<string, object>(AdditionalAttributes);
            attributes.Remove("href");
            AdditionalAttributes = attributes;
        }
    }
}

/// <summary>
/// Specifies the shape of the anchor, which can be rounded or square.
/// </summary>
public enum AnchorShape {
    /// <summary>
    /// The anchor has rounded corners.
    /// </summary>
    Round,
    /// <summary>
    /// The anchor has square corners.
    /// </summary>
    Square
}

/// <summary>
///     Specifies the appearance of an anchor element.
/// </summary>
public enum AnchorAppearance {

    /// <summary>
    ///     The anchor element is underlined.
    /// </summary>
    Underlined,

    /// <summary>
    ///     The anchor element is filled.
    /// </summary>
    Filled,

    /// <summary>
    ///     The anchor element is outlined.
    /// </summary>
    Outlined,

    /// <summary>
    ///     The anchor element has no special appearance.
    /// </summary>
    None
}
