using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a tooltip component that displays contextual information when users hover over or focus on an element.
/// </summary>
public partial class TnTTooltip : TnTPageScriptComponent<TnTTooltip> {
    internal string ResolvedElementId => ElementId ?? $"{ComponentIdentifier}-tooltip";

    /// <summary>
    ///     The background color of the component.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceVariant;

    /// <summary>
    ///     The border color of the component.
    /// </summary>
    [Parameter]
    public TnTColor BorderColor { get; set; } = TnTColor.Outline;

    /// <summary>
    ///     The content to be displayed inside the tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     The delay in milliseconds before the tooltip appears when hovering over the parent element.
    /// </summary>
    [Parameter]
    public int ShowDelay { get; set; } = 500;

    /// <summary>
    ///     The delay in milliseconds before the tooltip disappears when the mouse leaves the parent element.
    /// </summary>
    [Parameter]
    public int HideDelay { get; set; } = 200;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-tooltip")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("tnt-tooltip-background-color", BackgroundColor)
        .AddVariable("tnt-tooltip-text-color", TextColor)
        .AddVariable("tnt-tooltip-border-color", BorderColor)
        .AddVariable("tnt-tooltip-show-delay", $"{ShowDelay}ms")
        .AddVariable("tnt-tooltip-hide-delay", $"{HideDelay}ms")
        .Build();

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Tooltip/TnTTooltip.razor.js";

    /// <summary>
    ///     The text color of the component.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurfaceVariant;
}
