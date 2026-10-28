namespace NTComponents;

/// <summary>
///     Panel corner placement for the <see cref="NTPopoutTab" /> pop-out button.
/// </summary>
public enum NTPopoutTabButtonPlacement {
    /// <summary>
    ///     Top corner at the inline end of the panel.
    /// </summary>
    TopEnd,

    /// <summary>
    ///     Top corner at the inline start of the panel.
    /// </summary>
    TopStart,

    /// <summary>
    ///     Bottom corner at the inline end of the panel.
    /// </summary>
    BottomEnd,

    /// <summary>
    ///     Bottom corner at the inline start of the panel.
    /// </summary>
    BottomStart,

    /// <summary>
    ///     No pop-out button. Use the tab header context menu or bind <see cref="NTPopoutTab.PoppedOut" /> instead.
    /// </summary>
    None
}
