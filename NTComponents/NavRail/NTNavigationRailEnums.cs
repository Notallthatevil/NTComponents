namespace NTComponents;

/// <summary>
///     Behavior represented when a rail leaves expanded presentation.
/// </summary>
public enum RailCollapseBehavior {

    /// <summary>
    ///     The expanded rail collapses back to the persistent collapsed rail.
    /// </summary>
    Rail,

    /// <summary>
    ///     The collapsed rail is hidden, and the external menu button opens the rail as a temporary modal surface.
    /// </summary>
    Hide
}

/// <summary>
///     Active indicator width treatment for expanded rail destinations.
/// </summary>
public enum ActiveLinkIndicatorStyle {

    /// <summary>
    ///     Indicator hugs the icon and label content.
    /// </summary>
    HugContent,

    /// <summary>
    ///     Indicator fills the destination container width while the target remains unchanged.
    /// </summary>
    FullWidth
}

/// <summary>
///     Visual shape treatment for active navigation rail indicators.
/// </summary>
public enum NTNavigationRailVariant {

    /// <summary>
    ///     Pill-shaped active indicators.
    /// </summary>
    Pill,

    /// <summary>
    ///     Square active indicators.
    /// </summary>
    Square
}
