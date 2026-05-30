namespace NTComponents;

/// <summary>
///     Represents the Material 3 motion easing curves exposed by NTComponents.
/// </summary>
/// <remarks>
///     <para>These values map to the legacy Material 3 easing system used for transitions on the web.</para>
///     <para>
///         Objects in the physical world do not start or stop instantaneously. Easing gives transitions a more natural feel by shaping how motion speeds up and slows down, instead of appearing stiff
///         or mechanical.
///     </para>
///     <para>Material 3 easing is more expressive than Material 2, with snappier takeoffs and softer landings. The emphasized easing set is recommended for most transitions to capture that style.</para>
///     <para>
///         The standard easing set should be used for smaller utility-focused transitions that need to stay quick. It is also the fallback set for platforms that do not support emphasized easing
///         directly, including web.
///     </para>
///     <para>
///         As a practical default, Material 3 pairs <see cref="Emphasized" /> with 500ms for transitions that begin and end on screen, <see cref="EmphasizedDecelerate" /> with 400ms for entering
///         transitions, and <see cref="EmphasizedAccelerate" /> with 200ms for permanent exits. The equivalent standard pairings are 300ms, 250ms, and 200ms.
///     </para>
///     <para>On the web, the Material 3 spec does not define a standalone CSS curve for emphasized easing, so <see cref="Emphasized" /> falls back to the standard curve.</para>
/// </remarks>
public enum NTMotionEasing {

    /// <summary>
    ///     Standard fallback used for emphasized easing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for transitions that begin and end on screen, or for transitions that exit temporarily and should feel retrievable just off screen. Do not use it for permanent exits that
    ///         should leave at peak velocity.
    ///     </para>
    ///     <para>On the web this maps to the standard fallback curve, because the Material 3 spec does not provide a standalone CSS emphasized curve.</para>
    /// </remarks>
    Emphasized,

    /// <summary>
    ///     Emphasized easing for entering motion that begins at peak velocity and settles gently.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for entering motion that begins at peak velocity and comes to a gentle rest on screen. Do not use it for permanent exits or transitions that should end off screen.</para>
    /// </remarks>
    EmphasizedDecelerate,

    /// <summary>
    ///     Emphasized easing for exiting motion that begins at rest and ends at peak velocity.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for expressive exits, dismissals, or collapses that should feel permanent. Do not use it when the exiting content should feel retrievable or come to rest on screen.</para>
    /// </remarks>
    EmphasizedAccelerate,

    /// <summary>
    ///     Standard easing for simple utility-focused transitions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for simple, small, or utility-focused transitions that begin and end on screen. Do not use it when the transition should read as especially expressive if emphasized easing
    ///         is available.
    ///     </para>
    /// </remarks>
    Standard,

    /// <summary>
    ///     Standard easing for entering motion.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for standard enter transitions when a small, quick, utility-focused style is appropriate. Do not use it for permanent exits.</para>
    /// </remarks>
    StandardDecelerate,

    /// <summary>
    ///     Standard easing for exiting motion.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for standard exit transitions when a quick utility-focused style is appropriate. Do not use it when content should settle gently on screen.</para>
    /// </remarks>
    StandardAccelerate
}

/// <summary>
///     Provides extension methods for <see cref="NTMotionEasing" />.
/// </summary>
public static class NTMotionEasingExt {

    /// <summary>
    ///     Converts the easing value to its corresponding CSS utility class.
    /// </summary>
    /// <param name="easing">The easing value.</param>
    /// <returns>The CSS utility class for the easing value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="easing" /> is not a valid <see cref="NTMotionEasing" /> value.</exception>
    public static string ToCssClass(this NTMotionEasing easing) {
        return easing switch {
            NTMotionEasing.Emphasized => "nt-motion-easing-emphasized",
            NTMotionEasing.EmphasizedDecelerate => "nt-motion-easing-emphasized-decelerate",
            NTMotionEasing.EmphasizedAccelerate => "nt-motion-easing-emphasized-accelerate",
            NTMotionEasing.Standard => "nt-motion-easing-standard",
            NTMotionEasing.StandardDecelerate => "nt-motion-easing-standard-decelerate",
            NTMotionEasing.StandardAccelerate => "nt-motion-easing-standard-accelerate",
            _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, null)
        };
    }
}