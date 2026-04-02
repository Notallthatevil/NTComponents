using NTComponents;

namespace NTComponents.Core;

/// <summary>
/// Provides global default options for NTComponents, allowing host applications to configure baseline behavior across
/// all components.
/// </summary>
public class NTComponentsDefaultOptions {
    /// <summary>
    /// Gets the built-in fallback defaults used when no configured options instance has been registered in DI.
    /// </summary>
    public static readonly NTComponentsDefaultOptions Default = new();

    /// <summary>
    /// Gets or sets the default <see cref="FormAppearance"/> applied to form components when no explicit appearance is
    /// specified. Defaults to <see cref="FormAppearance.OutlinedCompact"/>.
    /// </summary>
    public FormAppearance DefaultFormAppearance { get; set; } = FormAppearance.OutlinedCompact;

    /// <summary>
    /// Gets or sets the default configuration for the NT layout shell components.
    /// </summary>
    public NTLayoutDefaults Layout { get; set; } = new();
}

/// <summary>
/// Global defaults for the NT layout shell components.
/// </summary>
public class NTLayoutDefaults {
    /// <summary>
    /// Gets or sets the default background color for <see cref="NTBody" />.
    /// </summary>
    public TnTColor BodyBackgroundColor { get; set; } = TnTColor.Background;

    /// <summary>
    /// Gets or sets whether <see cref="NTBody" /> scrolls by default.
    /// </summary>
    public bool BodyScrollable { get; set; } = true;

    /// <summary>
    /// Gets or sets the default tag name for <see cref="NTBody" />.
    /// </summary>
    public string BodyTagName { get; set; } = "main";

    /// <summary>
    /// Gets or sets the default text color for <see cref="NTBody" />.
    /// </summary>
    public TnTColor BodyTextColor { get; set; } = TnTColor.OnBackground;

    /// <summary>
    /// Gets or sets the default footer background color.
    /// </summary>
    public TnTColor FooterBackgroundColor { get; set; } = TnTColor.SurfaceContainerLow;

    /// <summary>
    /// Gets or sets the default footer elevation.
    /// </summary>
    public int FooterElevation { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether the footer is sticky by default.
    /// </summary>
    public bool FooterSticky { get; set; }

    /// <summary>
    /// Gets or sets the default tag name for <see cref="NTFooter" />.
    /// </summary>
    public string FooterTagName { get; set; } = "footer";

    /// <summary>
    /// Gets or sets the default footer text color.
    /// </summary>
    public TnTColor FooterTextColor { get; set; } = TnTColor.OnSurfaceVariant;

    /// <summary>
    /// Gets or sets whether <see cref="NTLayout" /> fills the viewport by default.
    /// </summary>
    public bool FillViewport { get; set; }

    /// <summary>
    /// Gets or sets the default header background color.
    /// </summary>
    public TnTColor HeaderBackgroundColor { get; set; } = TnTColor.Surface;

    /// <summary>
    /// Gets or sets whether the header is sticky by default.
    /// </summary>
    public bool HeaderSticky { get; set; }

    /// <summary>
    /// Gets or sets the default tag name for <see cref="NTHeader" />.
    /// </summary>
    public string HeaderTagName { get; set; } = "header";

    /// <summary>
    /// Gets or sets the default header text color.
    /// </summary>
    public TnTColor HeaderTextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    /// Gets or sets the default background color for <see cref="NTLayout" />.
    /// </summary>
    public TnTColor LayoutBackgroundColor { get; set; } = TnTColor.Background;

    /// <summary>
    /// Gets or sets the default layout mode.
    /// </summary>
    public NTLayoutMode Mode { get; set; } = NTLayoutMode.Root;

    /// <summary>
    /// Gets or sets the default tag name for <see cref="NTLayout" />.
    /// </summary>
    public string LayoutTagName { get; set; } = "div";

    /// <summary>
    /// Gets or sets the default text color for <see cref="NTLayout" />.
    /// </summary>
    public TnTColor LayoutTextColor { get; set; } = TnTColor.OnBackground;
}
