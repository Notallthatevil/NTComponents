using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a navigation link (anchor) component specifically designed for side navigation menus.
/// </summary>
[Obsolete("Use NTSideNavLink instead. TnTSideNavLink will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTSideNavLink {

    /// <inheritdoc />
    [Parameter]
    public virtual TnTColor? ActiveBackgroundColor { get; set; } = TnTColor.PrimaryContainer;

    /// <inheritdoc />
    [Parameter]
    public virtual TnTColor? ActiveTextColor { get; set; } = TnTColor.OnPrimaryContainer;

    /// <inheritdoc />
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <inheritdoc />
    [Parameter]
    public virtual TnTColor BackgroundColor { get; set; } = TnTColor.SecondaryContainer;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public ElementReference Element { get; protected set; }

    /// <inheritdoc />
    public virtual string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass(CssClass)
        .AddClass("tnt-side-nav-link")
        .AddClass("tnt-side-nav-tint-color", TintColor.HasValue)
        .AddClass("tnt-side-nav-on-tint-color", OnTintColor.HasValue)
        .AddDisabled(Disabled)
        .AddClass("tnt-interactable")
        .AddRipple(EnableRipple)
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
        .AddVariable("tnt-side-nav-active-bg-color", ActiveBackgroundColor.GetValueOrDefault(), ActiveBackgroundColor.HasValue)
        .AddVariable("tnt-side-nav-active-fg-color", ActiveTextColor.GetValueOrDefault(), ActiveTextColor.HasValue)
        .AddVariable("tnt-side-nav-bg-color", BackgroundColor)
        .AddVariable("tnt-side-nav-fg-color", TextColor)
        .AddVariable("tnt-side-nav-tint-color", TintColor.GetValueOrDefault(), TintColor.HasValue)
        .AddVariable("tnt-side-nav-on-tint-color", OnTintColor.GetValueOrDefault(), OnTintColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     The icon to show at the start of the link.
    /// </summary>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <inheritdoc />
    [Parameter]
    public virtual TnTColor TextColor { get; set; } = TnTColor.Secondary;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; } = TnTColor.SurfaceTint;

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
