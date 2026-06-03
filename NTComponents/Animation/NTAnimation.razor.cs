using System.Globalization;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Static SSR-friendly animation wrapper for revealing child content with Material 3 motion tokens.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NTAnimation" /> renders its child content immediately, then uses a small browser enhancement to observe viewport intersection and toggle animation state. Without JavaScript,
///         the content remains visible and usable.
///     </para>
///     <para>
///         Use <see cref="AnimateOut" /> when the content should reverse its transition after leaving the viewport. When <see cref="Once" /> is true and <see cref="AnimateOut" /> is false, the
///         wrapper reveals once and stops observing.
///     </para>
/// </remarks>
public partial class NTAnimation : TnTComponentBase {
    private const string AnimationJsModulePath = "./_content/NTComponents/Animation/NTAnimation.razor.js";

    /// <summary>
    ///     Animation used when the wrapper enters the viewport.
    /// </summary>
    [Parameter]
    public NTAnimationType Animation { get; set; } = NTAnimationType.FadeIn;

    /// <summary>
    ///     Whether the wrapper should animate back out when it leaves the viewport.
    /// </summary>
    [Parameter]
    public bool AnimateOut { get; set; }

    /// <summary>
    ///     The child content to animate.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Delay applied before enter and exit transitions start.
    /// </summary>
    [Parameter]
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-animation")
        .AddClass(Animation.ToCssClass())
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-animation-enter-duration", $"{EnterDuration.ToMilliseconds()}ms")
        .AddVariable("nt-animation-exit-duration", $"{ExitDuration.ToMilliseconds()}ms")
        .AddVariable("nt-animation-enter-easing", EnterEasing.ToCssValue())
        .AddVariable("nt-animation-exit-easing", ExitEasing.ToCssValue())
        .AddVariable("nt-animation-delay", $"{Math.Max(0, Delay.TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture)}ms")
        .AddVariable("nt-animation-distance", Distance)
        .Build();

    /// <summary>
    ///     Duration token used when the wrapper enters the viewport.
    /// </summary>
    [Parameter]
    public NTMotionDuration EnterDuration { get; set; } = NTMotionDuration.Ms400;

    /// <summary>
    ///     Easing token used when the wrapper enters the viewport.
    /// </summary>
    [Parameter]
    public NTMotionEasing EnterEasing { get; set; } = NTMotionEasing.EmphasizedDecelerate;

    /// <summary>
    ///     Duration token used when <see cref="AnimateOut" /> is enabled and the wrapper leaves the viewport.
    /// </summary>
    [Parameter]
    public NTMotionDuration ExitDuration { get; set; } = NTMotionDuration.Ms200;

    /// <summary>
    ///     Easing token used when <see cref="AnimateOut" /> is enabled and the wrapper leaves the viewport.
    /// </summary>
    [Parameter]
    public NTMotionEasing ExitEasing { get; set; } = NTMotionEasing.EmphasizedAccelerate;

    /// <summary>
    ///     Slide distance used by slide-in animations.
    /// </summary>
    [Parameter]
    public string Distance { get; set; } = "24px";

    /// <summary>
    ///     Whether the wrapper should stop observing after its first enter animation.
    /// </summary>
    [Parameter]
    public bool Once { get; set; } = true;

    /// <summary>
    ///     Margin applied to the IntersectionObserver root.
    /// </summary>
    [Parameter]
    public string RootMargin { get; set; } = "0px";

    /// <summary>
    ///     Percentage of the wrapper that must intersect before the enter state is applied.
    /// </summary>
    [Parameter]
    public double Threshold { get; set; } = 0.35;

    internal string JsModulePath => AnimationJsModulePath;
    private string ThresholdValue => Math.Clamp(Threshold, 0, 1).ToString("0.###", CultureInfo.InvariantCulture);
}

/// <summary>
///     Animation presets supported by <see cref="NTAnimation" />.
/// </summary>
public enum NTAnimationType {

    /// <summary>
    ///     Fades content in without directional movement.
    /// </summary>
    FadeIn,

    /// <summary>
    ///     Fades content in while moving it down from above.
    /// </summary>
    SlideInFromTop,

    /// <summary>
    ///     Fades content in while moving it up from below.
    /// </summary>
    SlideInFromBottom,

    /// <summary>
    ///     Fades content in while moving it right from the inline start side.
    /// </summary>
    SlideInFromLeft,

    /// <summary>
    ///     Fades content in while moving it left from the inline end side.
    /// </summary>
    SlideInFromRight
}

/// <summary>
///     Provides extension methods for <see cref="NTAnimationType" />.
/// </summary>
public static class NTAnimationTypeExtensions {

    /// <summary>
    ///     Converts an animation preset to its CSS class.
    /// </summary>
    public static string ToCssClass(this NTAnimationType animation) {
        return animation switch {
            NTAnimationType.FadeIn => "nt-animation-fade-in",
            NTAnimationType.SlideInFromTop => "nt-animation-slide-in-from-top",
            NTAnimationType.SlideInFromBottom => "nt-animation-slide-in-from-bottom",
            NTAnimationType.SlideInFromLeft => "nt-animation-slide-in-from-left",
            NTAnimationType.SlideInFromRight => "nt-animation-slide-in-from-right",
            _ => throw new ArgumentOutOfRangeException(nameof(animation), animation, null)
        };
    }
}

internal static class NTMotionEasingCssValueExtensions {
    internal static string ToCssValue(this NTMotionEasing easing) {
        return easing switch {
            NTMotionEasing.Emphasized => "cubic-bezier(0.2, 0, 0, 1)",
            NTMotionEasing.EmphasizedDecelerate => "cubic-bezier(0.05, 0.7, 0.1, 1)",
            NTMotionEasing.EmphasizedAccelerate => "cubic-bezier(0.3, 0, 0.8, 0.15)",
            NTMotionEasing.Standard => "cubic-bezier(0.2, 0, 0, 1)",
            NTMotionEasing.StandardDecelerate => "cubic-bezier(0, 0, 0, 1)",
            NTMotionEasing.StandardAccelerate => "cubic-bezier(0.3, 0, 1, 1)",
            _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, null)
        };
    }
}
