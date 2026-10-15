namespace NTComponents;

/// <summary>
///     What a <see cref="NTAnimationTrigger.Visible" /> <see cref="NTAnimation" /> does after it has been revealed.
/// </summary>
public enum NTAnimationRepeat {

    /// <summary>
    ///     Reveals once and stays visible.
    /// </summary>
    Once,

    /// <summary>
    ///     Plays the exit transition when the content drops below the threshold, and enters again when it comes back.
    /// </summary>
    Reverse,

    /// <summary>
    ///     Resets instantly once the content is fully out of view, and plays the enter animation again on the next reveal.
    /// </summary>
    Replay
}
