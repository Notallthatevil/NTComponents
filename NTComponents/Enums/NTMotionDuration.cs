namespace NTComponents;

/// <summary>
///     Represents the Material 3 motion duration tokens exposed by NTComponents.
/// </summary>
/// <remarks>
///     <para>These values expose the Material 3 duration tokens by their concrete millisecond values so the API is explicit about timing.</para>
///     <para>
///         Transitions should not be so short that they become jarring, or so slow that users feel as though they are waiting. The right combination of duration and easing should feel smooth,
///         responsive, and easy to follow.
///     </para>
///     <para>
///         Choose shorter durations for transitions that cover small areas of the screen, and longer durations for transitions that traverse larger areas. Scaling duration with the size of the
///         transition helps preserve a consistent sense of speed.
///     </para>
///     <para>
///         Exit, dismiss, and collapse transitions should generally use shorter durations, because they require less attention than the next user task. Entering or persistent transitions should
///         generally use longer durations so users can focus on what is newly appearing on screen.
///     </para>
/// </remarks>
public enum NTMotionDuration {

    /// <summary>
    ///     Duration token for 50ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this only for the smallest utility transitions when motion should be nearly instantaneous. Do not use it for larger transitions, where it is likely to feel jarring.</para>
    /// </remarks>
    Ms50,

    /// <summary>
    ///     Duration token for 100ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for very small and fast utility motion. Do not use it for transitions that need time to establish spatial context.</para>
    /// </remarks>
    Ms100,

    /// <summary>
    ///     Duration token for 150ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for short utility-focused transitions and quick effects. Do not use it when the movement covers enough distance that the transition becomes hard to follow.</para>
    /// </remarks>
    Ms150,

    /// <summary>
    ///     Duration token for 200ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for small-area transitions and fast exit motion. Do not use it for large entering transitions that should give users time to focus on new content.</para>
    /// </remarks>
    Ms200,

    /// <summary>
    ///     Duration token for 250ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for standard enter transitions over a modest area or for slightly longer utility motion. Do not use it when the transition should feel especially large or expressive.</para>
    /// </remarks>
    Ms250,

    /// <summary>
    ///     Duration token for 300ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this as a general default for standard transitions that begin and end on screen. Do not use it when a shorter exit or longer expressive duration is more appropriate.</para>
    /// </remarks>
    Ms300,

    /// <summary>
    ///     Duration token for 350ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for medium-size transitions that need slightly more travel time than a default 300ms transition. Do not use it for tiny utility effects.</para>
    /// </remarks>
    Ms350,

    /// <summary>
    ///     Duration token for 400ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for expressive enter transitions or transitions that cover a visibly larger area. Do not use it for quick dismissals.</para>
    /// </remarks>
    Ms400,

    /// <summary>
    ///     Duration token for 450ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for large transitions that should feel deliberate without becoming slow. Do not use it for small utility interactions.</para>
    /// </remarks>
    Ms450,

    /// <summary>
    ///     Duration token for 500ms.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for large, expressive transitions such as hero moments or transitions spanning a large screen area. Do not use it for common small transitions where it will feel sluggish.
    ///     </para>
    /// </remarks>
    Ms500,

    /// <summary>
    ///     Duration token for 550ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when a large transition needs slightly more time than 500ms to remain readable. Do not use it for frequent utility-focused motion.</para>
    /// </remarks>
    Ms550,

    /// <summary>
    ///     Duration token for 600ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for the upper end of large expressive transitions. Do not use it where users would feel blocked waiting for common UI interactions to finish.</para>
    /// </remarks>
    Ms600,

    /// <summary>
    ///     Duration token for 700ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for rare extra-long transitions, typically ambient or non-urgent motion. Do not use it for direct user-triggered utility interactions.</para>
    /// </remarks>
    Ms700,

    /// <summary>
    ///     Duration token for 800ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for rare ambient motion that can unfold more slowly without blocking the user. Do not use it for standard navigation or component transitions.</para>
    /// </remarks>
    Ms800,

    /// <summary>
    ///     Duration token for 900ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this only for rare extra-long ambient transitions. Do not use it for regular app interactions.</para>
    /// </remarks>
    Ms900,

    /// <summary>
    ///     Duration token for 1000ms.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this only for exceptional ambient transitions, such as auto-advance motion that does not depend on immediate user response. Do not use it for standard interactive transitions.</para>
    /// </remarks>
    Ms1000
}

/// <summary>
///     Provides extension methods for <see cref="NTMotionDuration" />.
/// </summary>
public static class NTMotionDurationExt {

    /// <summary>
    ///     Converts the duration token to its corresponding CSS utility class.
    /// </summary>
    /// <param name="duration">The duration token.</param>
    /// <returns>The CSS utility class for the duration token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="duration" /> is not a valid <see cref="NTMotionDuration" /> value.</exception>
    public static string ToCssClass(this NTMotionDuration duration) {
        return duration switch {
            NTMotionDuration.Ms50 => "nt-motion-duration-50",
            NTMotionDuration.Ms100 => "nt-motion-duration-100",
            NTMotionDuration.Ms150 => "nt-motion-duration-150",
            NTMotionDuration.Ms200 => "nt-motion-duration-200",
            NTMotionDuration.Ms250 => "nt-motion-duration-250",
            NTMotionDuration.Ms300 => "nt-motion-duration-300",
            NTMotionDuration.Ms350 => "nt-motion-duration-350",
            NTMotionDuration.Ms400 => "nt-motion-duration-400",
            NTMotionDuration.Ms450 => "nt-motion-duration-450",
            NTMotionDuration.Ms500 => "nt-motion-duration-500",
            NTMotionDuration.Ms550 => "nt-motion-duration-550",
            NTMotionDuration.Ms600 => "nt-motion-duration-600",
            NTMotionDuration.Ms700 => "nt-motion-duration-700",
            NTMotionDuration.Ms800 => "nt-motion-duration-800",
            NTMotionDuration.Ms900 => "nt-motion-duration-900",
            NTMotionDuration.Ms1000 => "nt-motion-duration-1000",
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
        };
    }

    /// <summary>
    ///     Converts the motion duration enum value to milliseconds.
    /// </summary>
    /// <param name="duration">The motion duration to convert.</param>
    /// <returns>The duration in milliseconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="duration" /> is not a valid <see cref="NTMotionDuration" /> value.</exception>
    public static int ToMilliseconds(this NTMotionDuration duration) {
        return duration switch {
            NTMotionDuration.Ms50 => 50,
            NTMotionDuration.Ms100 => 100,
            NTMotionDuration.Ms150 => 150,
            NTMotionDuration.Ms200 => 200,
            NTMotionDuration.Ms250 => 250,
            NTMotionDuration.Ms300 => 300,
            NTMotionDuration.Ms350 => 350,
            NTMotionDuration.Ms400 => 400,
            NTMotionDuration.Ms450 => 450,
            NTMotionDuration.Ms500 => 500,
            NTMotionDuration.Ms550 => 550,
            NTMotionDuration.Ms600 => 600,
            NTMotionDuration.Ms700 => 700,
            NTMotionDuration.Ms800 => 800,
            NTMotionDuration.Ms900 => 900,
            NTMotionDuration.Ms1000 => 1000,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
        };
    }
}