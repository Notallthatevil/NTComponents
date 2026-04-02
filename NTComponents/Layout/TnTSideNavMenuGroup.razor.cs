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
///     A collapsible navigation group component for side navigation menus.
/// </summary>
/// <remarks>Provides a toggleable group of navigation items with an expandable/collapsible section.</remarks>
[Obsolete("Use NTSideNavMenuGroup instead. TnTSideNavMenuGroup will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTSideNavMenuGroup {

    /// <inheritdoc />
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceVariant;

    /// <summary>
    ///     Gets or sets the content of the navigation group.
    /// </summary>
    /// <value>The child elements to be rendered inside the navigation group.</value>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-side-nav-menu-group-label")
        .AddClass("tnt-side-nav-menu-group-tint-color", TintColor.HasValue)
        .AddClass("tnt-side-nav-menu-group-on-tint-color", OnTintColor.HasValue)
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
        .AddVariable("tnt-side-nav-menu-group-bg-color", BackgroundColor)
        .AddVariable("tnt-side-nav-menu-group-fg-color", TextColor)
        .AddVariable("tnt-side-nav-menu-group-tint-color", TintColor.GetValueOrDefault(), TintColor.HasValue)
        .AddVariable("tnt-side-nav-menu-group-on-tint-color", OnTintColor.GetValueOrDefault(), OnTintColor.HasValue)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public bool EnableRipple { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the group is expanded by default.
    /// </summary>
    /// <value><c>true</c> if the group should be expanded when initially rendered; otherwise, <c>false</c>. Default is <c>true</c>.</value>
    [Parameter]
    public bool ExpandByDefault { get; set; } = true;

    /// <summary>
    ///     Gets or sets the icon displayed alongside the label.
    /// </summary>
    /// <value>The icon to display, or <c>null</c> if no icon should be shown.</value>
    [Parameter]
    public TnTIcon? Icon { get; set; }

    /// <summary>
    ///     Gets or sets the text label for the navigation group.
    /// </summary>
    /// <value>The text to display for the navigation group header.</value>
    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? OnTintColor { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TextAlign? TextAlignment { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurfaceVariant;

    /// <inheritdoc />
    [Parameter]
    public TnTColor? TintColor { get; set; } = TnTColor.SurfaceTint;
}
