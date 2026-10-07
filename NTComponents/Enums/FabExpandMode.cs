namespace NTComponents;

/// <summary>
/// Specifies the expansion mode of the FAB container.
/// </summary>
[Obsolete("FabExpandMode is obsolete and retained for legacy components.")]
public enum FabExpandMode {
    /// <summary>
    /// The FABs are always visible.
    /// </summary>
    Never,
    /// <summary>
    /// The FABs are always hidden under a toggle button.
    /// </summary>
    Always,
    /// <summary>
    /// The FABs are hidden under a toggle button on medium screens and down.
    /// </summary>
    SmallScreens
}
