using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Animates the child content when it enters the viewport.
/// </summary>
[Obsolete("Use NTAnimation instead.")]
public partial class TnTAnimation {

    /// <summary>
    ///     The animation to apply when the element enters the viewport.
    /// </summary>
    [Parameter]
    public Animation AnimationType { get; set; } = Animation.FadeIn;

    /// <summary>
    ///     The child content to animate.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     The duration of the animation in milliseconds.
    /// </summary>
    [Parameter]
    public int Duration { get; set; } = 500;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-animation")
        .AddClass($"tnt-animation-{AnimationType.ToString().ToLower()}")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddStyle("animation-duration", $"{Duration}ms")
        .Build();

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Animation/TnTAnimation.razor.js";

    /// <summary>
    ///     The overlap amount of this element with the viewport needed to trigger the animation.
    /// </summary>
    [Parameter]
    public float Threshold { get; set; } = 0.5f;
}

/// <summary>
///     Specifies the available animation types for the <see cref="TnTAnimation" /> component.
/// </summary>
public enum Animation {

    /// <summary>
    ///     Fades the element in.
    /// </summary>
    FadeIn,

    /// <summary>
    ///     Fades the element in and slides it from the top.
    /// </summary>
    SlideTop,

    /// <summary>
    ///     Fades the element in and slides it from the bottom.
    /// </summary>
    SlideBottom,

    /// <summary>
    ///     Fades the element in and slides it from the left.
    /// </summary>
    SlideLeft,

    /// <summary>
    ///     Fades the element in and slides it from the right.
    /// </summary>
    SlideRight,

    /// <summary>
    ///     Flips the element horizontally.
    /// </summary>
    Flip,

    /// <summary>
    ///     Rotates the object 90 degrees clockwise.
    /// </summary>
    RotateClockwise,

    /// <summary>
    ///     Rotates the object 90 degrees counterclockwise.
    /// </summary>
    RotateCounterClockwise,
}
