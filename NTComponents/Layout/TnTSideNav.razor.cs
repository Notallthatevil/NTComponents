using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
///     Represents a side navigation component in the TnT layout system.
/// </summary>
[Obsolete("Use NTSideNav instead. TnTSideNav will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTSideNav {

    /// <inheritdoc />
    [Parameter]
    public override TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainer;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create(base.ElementClass!)
        .AddClass("tnt-side-nav")
        .AddClass("tnt-side-nav-hide-on-large", HideOnLargeScreens)
        .AddBackgroundColor(BackgroundColor)
        .AddForegroundColor(TextColor)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-side-nav-bg-color", BackgroundColor)
        .AddVariable("tnt-side-nav-fg-color", TextColor)
        .Build();

    /// <summary>
    ///     Indicates whether the side navigation should be hidden on large screens.
    /// </summary>
    [Parameter]
    public bool HideOnLargeScreens { get; set; }

    /// <inheritdoc />
    [Parameter]
    public override TnTColor TextColor { get; set; } = TnTColor.OnSurfaceVariant;
}
