using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.CodeDocumentation;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Static SSR-friendly animation wrapper that reveals child content with Material 3 motion patterns and tokens.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="NTAnimation" /> renders its child content immediately. The default <see cref="NTAnimationTrigger.Visible" /> trigger uses a small browser enhancement to reveal content
///         when it scrolls into view; without JavaScript the content stays visible. Content that is already on screen when the page loads is shown without animating, so use
///         <see cref="NTAnimationTrigger.Render" /> for above-the-fold entrances.
///     </para>
///     <para>
///         Set <see cref="Stagger" /> to animate each direct child in sequence instead of the wrapper as a whole. Reduced-motion users get a short fade without movement, delays, or staggering.
///         Hidden content is always shown when printing or when keyboard focus moves into it.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders content statically; Visible-triggered animation is enhanced with JavaScript, Render-triggered animation is CSS only.",
    CompatibilityDetails = "Child content renders in static SSR. The Visible trigger adds IntersectionObserver-driven animation after the page reaches the browser. The Render trigger uses CSS @starting-style and needs no script. The Manual trigger and the OnEntered and OnExited callbacks require interactivity.")]
public partial class NTAnimation : NTPageScriptComponent<NTAnimation> {
    private const NTMotionDuration DefaultEnterDuration = NTMotionDuration.Ms400;
    private const NTMotionEasing DefaultEnterEasing = NTMotionEasing.EmphasizedDecelerate;
    private const NTMotionDuration DefaultExitDuration = NTMotionDuration.Ms200;
    private const NTMotionEasing DefaultExitEasing = NTMotionEasing.EmphasizedAccelerate;

    /// <summary>
    ///     Motion pattern used when the content enters.
    /// </summary>
    [Parameter]
    public NTAnimationType Animation { get; set; } = NTAnimationType.FadeIn;

    /// <summary>
    ///     The child content to animate.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    ///     Delay applied before the enter transition starts. Exit transitions start immediately.
    /// </summary>
    [Parameter]
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     When true, the wrapper renders its content without any animation behavior.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     CSS length travelled by slide and shared-axis animations. Defaults to 24px for slides and 30px for shared-axis patterns.
    /// </summary>
    [Parameter]
    public string? Distance { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-animation")
        .AddClass(Animation.ToCssClass())
        .AddClass("nt-animation-stagger", Stagger > TimeSpan.Zero)
        .AddClass(EnterEasing != DefaultEnterEasing ? $"nt-animation-enter-{EasingName(EnterEasing)}" : null)
        .AddClass(ExitEasing != DefaultExitEasing ? $"nt-animation-exit-{EasingName(ExitEasing)}" : null)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-animation-enter-duration", EnterDuration != DefaultEnterDuration ? $"{EnterDuration.ToMilliseconds()}ms" : "", EnterDuration != DefaultEnterDuration)
        .AddVariable("nt-animation-exit-duration", ExitDuration != DefaultExitDuration ? $"{ExitDuration.ToMilliseconds()}ms" : "", ExitDuration != DefaultExitDuration)
        .AddVariable("nt-animation-delay", Delay > TimeSpan.Zero ? ToCssTime(Delay) : "", Delay > TimeSpan.Zero)
        .AddVariable("nt-animation-stagger", Stagger > TimeSpan.Zero ? ToCssTime(Stagger) : "", Stagger > TimeSpan.Zero)
        .AddVariable("nt-animation-distance", Distance!, !string.IsNullOrWhiteSpace(Distance))
        .Build();

    /// <summary>
    ///     Duration token used for the enter transition.
    /// </summary>
    [Parameter]
    public NTMotionDuration EnterDuration { get; set; } = DefaultEnterDuration;

    /// <summary>
    ///     Easing token used for the enter transition.
    /// </summary>
    [Parameter]
    public NTMotionEasing EnterEasing { get; set; } = DefaultEnterEasing;

    /// <summary>
    ///     Duration token used for the exit transition.
    /// </summary>
    [Parameter]
    public NTMotionDuration ExitDuration { get; set; } = DefaultExitDuration;

    /// <summary>
    ///     Easing token used for the exit transition.
    /// </summary>
    [Parameter]
    public NTMotionEasing ExitEasing { get; set; } = DefaultExitEasing;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Animation/NTAnimation.razor.js";

    /// <summary>
    ///     Raised when the content is revealed. Applies to the <see cref="NTAnimationTrigger.Visible" /> trigger and requires interactivity.
    /// </summary>
    [Parameter]
    public EventCallback OnEntered { get; set; }

    /// <summary>
    ///     Raised when revealed content is hidden again by <see cref="NTAnimationRepeat.Reverse" /> or <see cref="NTAnimationRepeat.Replay" />. Applies to the
    ///     <see cref="NTAnimationTrigger.Visible" /> trigger and requires interactivity.
    /// </summary>
    [Parameter]
    public EventCallback OnExited { get; set; }

    /// <summary>
    ///     What happens after the content has been revealed and then leaves the viewport. Applies to the <see cref="NTAnimationTrigger.Visible" /> trigger.
    /// </summary>
    [Parameter]
    public NTAnimationRepeat Repeat { get; set; } = NTAnimationRepeat.Once;

    /// <summary>
    ///     Margin applied to the IntersectionObserver root, using CSS margin syntax with px or % units. Applies to the <see cref="NTAnimationTrigger.Visible" /> trigger.
    /// </summary>
    [Parameter]
    public string RootMargin { get; set; } = "0px";

    /// <summary>
    ///     Whether the content is shown. Applies to the <see cref="NTAnimationTrigger.Manual" /> trigger; hidden content keeps its layout space and is made inert.
    /// </summary>
    [Parameter]
    public bool Shown { get; set; } = true;

    /// <summary>
    ///     When greater than zero, each direct child animates individually, delayed by its index times this value.
    /// </summary>
    [Parameter]
    public TimeSpan Stagger { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     HTML tag rendered for the wrapper.
    /// </summary>
    [Parameter]
    public NTAnimationTag Tag { get; set; } = NTAnimationTag.Div;

    /// <summary>
    ///     Fraction (0 to 1) of the content, or of the viewport for content taller or wider than the viewport, that must be visible before it is revealed. Applies to the
    ///     <see cref="NTAnimationTrigger.Visible" /> trigger.
    /// </summary>
    [Parameter]
    public double Threshold { get; set; } = 0.35;

    /// <summary>
    ///     What starts the enter animation.
    /// </summary>
    [Parameter]
    public NTAnimationTrigger Trigger { get; set; } = NTAnimationTrigger.Visible;

    private bool UsesObserver => !Disabled && Trigger == NTAnimationTrigger.Visible;

    /// <summary>
    ///     Invoked by the browser module when the content is revealed.
    /// </summary>
    [JSInvokable]
    public Task NotifyEnteredAsync() => OnEntered.InvokeAsync();

    /// <summary>
    ///     Invoked by the browser module when revealed content is hidden again.
    /// </summary>
    [JSInvokable]
    public Task NotifyExitedAsync() => OnExited.InvokeAsync();

    private static string EasingName(NTMotionEasing easing) => easing.ToCssClass()["nt-motion-easing-".Length..];

    private static string ToCssTime(TimeSpan value) => $"{value.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)}ms";

    private Dictionary<string, object> WrapperAttributes {
        get {
            var attributes = AdditionalAttributes is null ? [] : new Dictionary<string, object>(AdditionalAttributes);
            Set("class", ElementClass);
            Set("style", ElementStyle);
            Set("id", ElementId);
            Set("lang", ElementLang);
            Set("title", ElementTitle);
            if (!Disabled) {
                Set("data-nt-animation-trigger", Trigger switch {
                    NTAnimationTrigger.Render => "render",
                    NTAnimationTrigger.Manual => "manual",
                    _ => "visible"
                });
            }
            if (UsesObserver) {
                Set("data-nt-animation-repeat", Repeat switch {
                    NTAnimationRepeat.Reverse => "reverse",
                    NTAnimationRepeat.Replay => "replay",
                    _ => "once"
                });
                Set("data-nt-animation-threshold", Math.Clamp(double.IsNaN(Threshold) ? 0 : Threshold, 0, 1).ToString("0.###", CultureInfo.InvariantCulture));
                Set("data-nt-animation-root-margin", RootMargin);
                Set("data-nt-animation-notify", OnEntered.HasDelegate || OnExited.HasDelegate ? true : null);
            }
            if (!Disabled && Trigger == NTAnimationTrigger.Manual) {
                Set("data-nt-animation-state", Shown ? "visible" : "hidden");
                Set("inert", Shown ? null : true);
            }
            return attributes;

            void Set(string name, object? value) {
                if (value is null) {
                    attributes.Remove(name);
                }
                else {
                    attributes[name] = value;
                }
            }
        }
    }
}

/// <summary>
///     Animation presets supported by <see cref="NTAnimation" />.
/// </summary>
public enum NTAnimationType {

    /// <summary>
    ///     Fades content in without movement. Material 3 fade pattern.
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
    ///     Fades content in while moving it right from the physical left side, regardless of text direction.
    /// </summary>
    SlideInFromLeft,

    /// <summary>
    ///     Fades content in while moving it left from the physical right side, regardless of text direction.
    /// </summary>
    SlideInFromRight,

    /// <summary>
    ///     Fades content in while moving it from the inline start side (left in LTR, right in RTL).
    /// </summary>
    SlideInFromStart,

    /// <summary>
    ///     Fades content in while moving it from the inline end side (right in LTR, left in RTL).
    /// </summary>
    SlideInFromEnd,

    /// <summary>
    ///     Fades content in while growing it from 80% scale.
    /// </summary>
    ScaleIn,

    /// <summary>
    ///     Material 3 fade through: content grows from 92% scale with a fade that starts after the first 30% of the duration. Use between elements without a strong relationship.
    /// </summary>
    FadeThrough,

    /// <summary>
    ///     Material 3 shared axis X: content moves in 30px from the inline end with a late fade. Use for forward and lateral navigation.
    /// </summary>
    SharedAxisX,

    /// <summary>
    ///     Material 3 shared axis Y: content moves up 30px with a late fade. Use for vertically related content such as steppers.
    /// </summary>
    SharedAxisY,

    /// <summary>
    ///     Material 3 shared axis Z: content grows from 80% scale with a late fade. Use for parent-child navigation.
    /// </summary>
    SharedAxisZ
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
            NTAnimationType.SlideInFromStart => "nt-animation-slide-in-from-start",
            NTAnimationType.SlideInFromEnd => "nt-animation-slide-in-from-end",
            NTAnimationType.ScaleIn => "nt-animation-scale-in",
            NTAnimationType.FadeThrough => "nt-animation-fade-through",
            NTAnimationType.SharedAxisX => "nt-animation-shared-axis-x",
            NTAnimationType.SharedAxisY => "nt-animation-shared-axis-y",
            NTAnimationType.SharedAxisZ => "nt-animation-shared-axis-z",
            _ => throw new ArgumentOutOfRangeException(nameof(animation), animation, null)
        };
    }
}
