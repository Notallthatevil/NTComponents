using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Defines the appearance options available for icon components.
/// </summary>
public enum IconAppearance {

    /// <summary>
    ///     Default icon style with standard rounded edges.
    /// </summary>
    Default,

    /// <summary>
    ///     Filled icon style for Material Symbols.
    /// </summary>
    Filled,

    /// <summary>
    ///     Outlined icon style with transparent fill and defined edges.
    /// </summary>
    Outlined,

    /// <summary>
    ///     Round icon style with fully rounded corners and edges.
    /// </summary>
    Round,

    /// <summary>
    ///     Sharp icon style with angular corners and straight edges.
    /// </summary>
    Sharp
}

/// <summary>
///     Defines the size options available for icon components.
/// </summary>
public enum IconSize {

    /// <summary>
    ///     Small icon size, suitable for compact UI elements.
    /// </summary>
    Small,

    /// <summary>
    ///     Medium icon size, the default size for most UI contexts.
    /// </summary>
    Medium,

    /// <summary>
    ///     Large icon size, suitable for prominent UI elements.
    /// </summary>
    Large,

    /// <summary>
    ///     Extra large icon size, suitable for featured or hero UI elements.
    /// </summary>
    ExtraLarge
}

/// <summary>
///     Specifies the type of icon to be used.
/// </summary>
public enum IconType {

    /// <summary>
    ///     Represents Material Icons.
    /// </summary>
    MaterialIcons,

    /// <summary>
    ///     Represents Font Awesome icons.
    /// </summary>
    FontAwesome
}

/// <summary>
///     Obsolete compatibility alias for <see cref="NTIcon" />.
/// </summary>
[Obsolete("TnTIcon is obsolete. Use NTIcon instead.")]
public abstract class TnTIcon : NTIcon, ITnTComponentBase { }
