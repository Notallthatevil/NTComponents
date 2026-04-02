using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
///     The header content to be rendered.
/// </summary>
[Obsolete("Use NTHeader instead. TnTHeader will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTHeader {

    /// <inheritdoc />
    [Parameter]
    public override TnTColor BackgroundColor { get; set; } = TnTColor.Surface;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create(base.ElementClass!)
        .AddClass("tnt-header")
        .AddBackgroundColor(BackgroundColor)
        .AddForegroundColor(TextColor)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-header-bg-color", BackgroundColor)
        .AddVariable("tnt-header-fg-color", TextColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public override TnTColor TextColor { get; set; } = TnTColor.OnSurface;

}
