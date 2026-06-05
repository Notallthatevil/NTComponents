using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
///     Represents a theme toggle component for switching between different themes.
/// </summary>
[Obsolete("TnTThemeToggle is obsolete. Use NTThemeToggle instead.")]
public partial class TnTThemeToggle {

    /// <summary>
    ///     If true, allows the user to select a contrast level (Default, Medium, High).
    /// </summary>
    [Parameter]
    public bool AllowContrastSelection { get; set; } = true;

    /// <summary>
    ///     If true, allows the user to select a theme (Light, Dark, System).
    /// </summary>
    [Parameter]
    public bool AllowThemeSelection { get; set; } = true;

    /// <summary>
    ///     The CSS file name for the dark theme with default contrast.
    /// </summary>
    [Parameter]
    public string DarkDefaultCss { get; set; } = "dark.css";

    /// <summary>
    ///     The CSS file name for the dark theme with high contrast.
    /// </summary>
    [Parameter]
    public string DarkHighCss { get; set; } = "dark-hc.css";

    /// <summary>
    ///     The CSS file name for the dark theme with medium contrast.
    /// </summary>
    [Parameter]
    public string DarkMediumCss { get; set; } = "dark-mc.css";

    /// <summary>
    ///     The default theme to use if no user preference is set.
    /// </summary>
    [Parameter]
    public Theme DefaultTheme { get; set; } = Theme.System;

    /// <inheritdoc />
    public override string? ElementClass => string.Empty;

    /// <inheritdoc />
    public override string? ElementStyle => string.Empty;

    /// <summary>
    ///     If set to true, hides the theme toggle component from view.
    /// </summary>
    [Parameter]
    public bool Hide { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Theming/TnTThemeToggle.razor.js?v=2";

    // Theme CSS file names (defaults)
    /// <summary>
    ///     The CSS file name for the light theme with default contrast.
    /// </summary>
    [Parameter]
    public string LightDefaultCss { get; set; } = "light.css";

    /// <summary>
    ///     The CSS file name for the light theme with high contrast.
    /// </summary>
    [Parameter]
    public string LightHighCss { get; set; } = "light-hc.css";

    /// <summary>
    ///     The CSS file name for the light theme with medium contrast.
    /// </summary>
    [Parameter]
    public string LightMediumCss { get; set; } = "light-mc.css";

    /// <summary>
    ///     The root path for theme CSS files.
    /// </summary>
    [Parameter]
    public string ThemesRoot { get; set; } = "/Themes";
}

/// <summary>
///     Represents the theme options available for the application.
/// </summary>
[Obsolete("Theme is obsolete. Use NTTheme instead.")]
public enum Theme {

    /// <summary>
    ///     Use the system's theme setting (follows the user's OS or browser preference).
    /// </summary>
    System,

    /// <summary>
    ///     Use the light theme.
    /// </summary>
    Light,

    /// <summary>
    ///     Use the dark theme.
    /// </summary>
    Dark
}
