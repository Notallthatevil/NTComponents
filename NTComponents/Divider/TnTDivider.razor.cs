using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a divider component with customizable color, margin, and orientation.
/// </summary>
[Obsolete("Use NTDivider instead.")]
public partial class TnTDivider {

    /// <summary>
    ///     Gets or sets the color of the divider.
    /// </summary>
    [Parameter]
    public TnTColor? Color { get; set; } = TnTColor.OutlineVariant;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddClass("tnt-divider")
            .AddClass($"tnt-divider-{Direction.ToString().ToLower()}")
            .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
            .AddVariable("tnt-divider-color", $"var(--tnt-color-{Color.ToCssClassName()})", Color.HasValue)
            .Build();

    /// <summary>
    ///     Gets or sets the orientation of the divider.
    /// </summary>
    [Parameter]
    public LayoutDirection Direction { get; set; } = LayoutDirection.Horizontal;

}
