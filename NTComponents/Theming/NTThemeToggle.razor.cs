using Microsoft.AspNetCore.Components;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Preferred NTComponents theme toggle component.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires browser JavaScript for theme selection and persistence.",
    CompatibilityDetails = "The toggle depends on its page script, theme stylesheet switching, storage, and system-theme media queries. Static SSR can only render the initial selector shell.")]
public partial class NTThemeToggle {

    /// <summary>
    ///     If true, allows the user to select a contrast level.
    /// </summary>
    [Parameter]
    public bool AllowContrastSelection { get; set; } = true;

    /// <summary>
    ///     If true, allows the user to select a theme mode.
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
    ///     Gets or sets the default contrast level when storage has no valid value.
    /// </summary>
    [Parameter]
    public NTThemeContrast DefaultContrast { get; set; } = NTThemeContrast.Default;

    /// <summary>
    ///     Gets or sets the default theme mode when storage has no valid value.
    /// </summary>
    [Parameter]
    public NTTheme DefaultTheme { get; set; } = NTTheme.System;

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
    public override string? JsModulePath => "./_content/NTComponents/Theming/NTThemeToggle.razor.js?v=1";

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

    private string DefaultContrastValue => NTThemeConfiguration.ToRuntimeValue(DefaultContrast);

    private string DefaultThemeValue => NTThemeConfiguration.ToRuntimeValue(DefaultTheme);
}
