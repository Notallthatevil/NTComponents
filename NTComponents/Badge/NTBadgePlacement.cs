namespace NTComponents;

/// <summary>
///     Specifies how an <see cref="NTBadge" /> positions itself relative to its host's anchor.
/// </summary>
public enum NTBadgePlacement {

    /// <summary>
    ///     Overlaps the top-end corner of an icon using the Material 3 badge offsets.
    /// </summary>
    Icon,

    /// <summary>
    ///     Centers on the anchor point so the badge straddles the host's edge, half on and half off the container.
    /// </summary>
    Edge,

    /// <summary>
    ///     Flows inline with surrounding content, such as after a menu item or tab label.
    /// </summary>
    Inline
}
