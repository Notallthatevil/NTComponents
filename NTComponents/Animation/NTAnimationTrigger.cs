namespace NTComponents;

/// <summary>
///     What starts an <see cref="NTAnimation" /> enter animation.
/// </summary>
public enum NTAnimationTrigger {

    /// <summary>
    ///     Reveals the content when enough of it scrolls into view. Content already on screen when the page loads is shown without animating. Enhanced with JavaScript; without it the
    ///     content stays visible.
    /// </summary>
    Visible,

    /// <summary>
    ///     Animates the content as soon as it is rendered, on page load or when inserted later, using CSS only. Use for above-the-fold entrances. Prerendered interactive components may
    ///     replay the animation when interactivity starts, because Blazor replaces the prerendered markup.
    /// </summary>
    Render,

    /// <summary>
    ///     Shows and hides the content from the <see cref="NTAnimation.Shown" /> parameter. Requires interactivity to change state after the first render.
    /// </summary>
    Manual
}
