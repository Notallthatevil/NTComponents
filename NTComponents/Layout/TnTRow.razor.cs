using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     A component that represents a row
/// </summary>
[Obsolete("TnTRow is obsolete.")]
public partial class TnTRow  {

    /// <inheritdoc />
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-row")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();
}
