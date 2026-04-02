using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Layout;

namespace NTComponents;

/// <summary>
///     The footer content to be rendered.
/// </summary>
[Obsolete("Use NTFooter instead. TnTFooter will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTFooter {

    /// <inheritdoc />
    [Parameter]
    public override TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceVariant;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create(base.ElementClass!)
        .AddClass("tnt-footer")
        .AddElevation(Elevation)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-footer-bg-color", BackgroundColor)
        .AddVariable("tnt-footer-fg-color", TextColor)
        .Build();

    /// <inheritdoc />
    [Parameter]
    public override int Elevation { get; set; } = 2;

    /// <inheritdoc />
    [Parameter]
    public override TnTColor TextColor { get; set; } = TnTColor.OnSurfaceVariant;
}
