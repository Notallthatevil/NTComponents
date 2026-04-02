using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Toggle button for the side navigation component.
/// </summary>
[Obsolete("Use NTSideNavToggle instead. TnTSideNavToggle will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTSideNavToggle {

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-side-nav-toggle")
        .AddDisabled(Disabled)
        .AddClass("tnt-interactable")
        .AddRipple(EnableRipple)
        .AddClass("tnt-side-nav-toggle-tint-color", TintColor.HasValue)
        .AddClass("tnt-side-nav-toggle-on-tint-color", OnTintColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public string? ElementName { get; set; }

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-side-nav-toggle-tint-color", TintColor.GetValueOrDefault(), TintColor.HasValue)
        .AddVariable("tnt-side-nav-toggle-on-tint-color", OnTintColor.GetValueOrDefault(), OnTintColor.HasValue)
        .AddVariable("tnt-side-nav-toggle-icon-color", IconColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     The icon to be displayed in the toggle button.
    /// </summary>
    [Parameter]
    public TnTIcon Icon { get; set; } = MaterialIcon.Menu;

    ///<summary>
    /// The color of the toggle icon.
    /// </summary>
    [Parameter]
    public TnTColor IconColor { get; set; } = TnTColor.OnSurface;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; } = TnTColor.SurfaceTint;
}
