namespace NTComponents;

/// <summary>
///     Describes the visible state of an <see cref="NTWindow" />.
/// </summary>
public enum NTWindowState {

    /// <summary>
    ///     The window renders at its normal size.
    /// </summary>
    Normal,

    /// <summary>
    ///     The window renders only its title bar and controls.
    /// </summary>
    Minimized,

    /// <summary>
    ///     The window fills the viewport.
    /// </summary>
    Fullscreen
}
