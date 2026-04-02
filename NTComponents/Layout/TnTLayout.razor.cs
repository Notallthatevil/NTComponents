using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a layout component that serves as a container for other components or content.
/// </summary>
[Obsolete("Use NTLayout instead. TnTLayout will be removed in a future release.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TnTLayout {

    /// <summary>
    ///     The child content to be rendered inside the layout component.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-layout")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
       .AddFromAdditionalAttributes(AdditionalAttributes)
       .Build();

}
