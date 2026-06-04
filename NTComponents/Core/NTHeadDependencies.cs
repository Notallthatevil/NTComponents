using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Meant to be placed in the head section of App.razor to include necessary dependencies for NTComponents.
/// </summary>
[ExcludeFromCodeCoverage]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders head dependency links during static SSR.",
    CompatibilityDetails = "The component emits stylesheet, font, and script dependencies as head content without needing an interactive render mode.")]
public class NTHeadDependencies : IComponent {
    private const string DefaultTokenScope = ":root";
    private const string DarkThemeMedia = "(prefers-color-scheme: dark)";
    private const string LightThemeMedia = "(prefers-color-scheme: light)";
    private RenderHandle _renderHandle;

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
    ///     Gets or sets the footer height.
    /// </summary>
    [Parameter]
    public double FooterHeight { get; set; } = 64;

    /// <summary>
    ///     Gets or sets the header height.
    /// </summary>
    [Parameter]
    public double HeaderHeight { get; set; } = 64;

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
    ///     Gets or sets the side navigation width.
    /// </summary>
    [Parameter]
    public double SideNavWidth { get; set; } = 256;

    /// <summary>
    ///     Gets or sets the root path for theme CSS files.
    /// </summary>
    [Parameter]
    public string ThemesRoot { get; set; } = "/Themes";

    /// <summary>
    ///     Gets or sets the selector where NT measurement tokens are emitted.
    /// </summary>
    [Parameter]
    public string TokenScopeSelector { get; set; } = DefaultTokenScope;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters) {
        parameters.SetParameterProperties(this);
        _renderHandle.Render(Render);
        return Task.CompletedTask;
    }

    internal static string NormalizeTokenScopeSelector(string? tokenScopeSelector) => string.IsNullOrWhiteSpace(tokenScopeSelector)
        ? DefaultTokenScope
        : tokenScopeSelector.Trim();

    internal static string CreateMeasurementTokens(double headerHeight, double footerHeight, double sideNavWidth, string? tokenScopeSelector) {
        return string.Create(CultureInfo.InvariantCulture, $"{NormalizeTokenScopeSelector(tokenScopeSelector)}{{--tnt-header-height:{headerHeight}px;--tnt-footer-height:{footerHeight}px;--tnt-side-nav-width:{sideNavWidth}px;}}");
    }

    internal static void RenderMeasurementTokens(RenderTreeBuilder builder, int sequence, double headerHeight, double footerHeight, double sideNavWidth, string? tokenScopeSelector) {
        builder.OpenElement(sequence, "style");
        builder.AddAttribute(sequence + 1, "class", "tnt-measurements");
        builder.AddContent(sequence + 2, CreateMeasurementTokens(headerHeight, footerHeight, sideNavWidth, tokenScopeSelector));
        builder.CloseElement();
    }

    internal static string CreateThemeStylesheetHref(string? themesRoot, string? cssFile) {
        var root = string.IsNullOrWhiteSpace(themesRoot) ? "/Themes" : themesRoot.Trim();
        var file = string.IsNullOrWhiteSpace(cssFile) ? "light.css" : cssFile.Trim();
        return $"{root.TrimEnd('/')}/{file.TrimStart('/')}";
    }

    private void Render(RenderTreeBuilder builder) {
        // <style data-tnt-theme-critical="true">html, body, #app { background-color: Canvas; color: CanvasText; }</style>
        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "data-tnt-theme-critical", "true");
        builder.AddAttribute(2, "data-nt-theme-critical", "true");
        builder.AddAttribute(3, "data-permanent", string.Empty);
        builder.AddContent(4, "html, body, #app { background-color: Canvas; color: CanvasText; }");
        builder.CloseElement();

        RenderFirstPaintThemeLinks(builder, 5);

        // <script type="application/json" id="nt-theme-config">...</script>
        builder.OpenElement(20, "script");
        builder.AddAttribute(21, "type", "application/json");
        builder.AddAttribute(22, "id", "nt-theme-config");
        builder.AddAttribute(23, "data-permanent", string.Empty);
        builder.AddMarkupContent(24, CreateThemeConfiguration().ToJson());
        builder.CloseElement();

        // <script type="application/json" id="nt-theme-state">...</script>
        builder.OpenElement(25, "script");
        builder.AddAttribute(26, "type", "application/json");
        builder.AddAttribute(27, "id", "nt-theme-state");
        builder.AddAttribute(28, "data-permanent", string.Empty);
        builder.AddMarkupContent(29, "{}");
        builder.CloseElement();

        // <script src="_content/NTComponents/NTTheme.runtime.js"></script>
        builder.OpenElement(30, "script");
        builder.AddAttribute(31, "src", "_content/NTComponents/NTTheme.runtime.js");
        builder.AddAttribute(32, "data-permanent", string.Empty);
        builder.CloseElement();

        // <script src="_content/NTComponents/theme-bootstrap.js"></script>
        builder.OpenElement(33, "script");
        builder.AddAttribute(34, "src", "_content/NTComponents/theme-bootstrap.js");
        builder.AddAttribute(35, "data-permanent", string.Empty);
        builder.CloseElement();

        // <style class="tnt-measurements">...</style>
        RenderMeasurementTokens(builder, 36, HeaderHeight, FooterHeight, SideNavWidth, TokenScopeSelector);

        // <link rel="stylesheet" href="_content/NTComponents/nt-measurements.css">
        builder.OpenElement(40, "link");
        builder.AddAttribute(41, "rel", "stylesheet");
        builder.AddAttribute(42, "href", "_content/NTComponents/nt-measurements.css");
        builder.AddAttribute(43, "data-permanent", string.Empty);
        builder.CloseElement();

        // <link rel="stylesheet" href="_content/NTComponents/nt-ripple.css">
        builder.OpenElement(44, "link");
        builder.AddAttribute(45, "rel", "stylesheet");
        builder.AddAttribute(46, "href", "_content/NTComponents/nt-ripple.css");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.googleapis.com">
        builder.OpenElement(47, "link");
        builder.AddAttribute(48, "rel", "preconnect");
        builder.AddAttribute(49, "href", "https://fonts.googleapis.com");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        builder.OpenElement(50, "link");
        builder.AddAttribute(51, "rel", "preconnect");
        builder.AddAttribute(52, "href", "https://fonts.gstatic.com");
        builder.AddAttribute(53, "crossorigin", string.Empty);
        builder.CloseElement();

        // <link href="https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap" rel="stylesheet">
        builder.OpenElement(54, "link");
        builder.AddAttribute(55, "href", "https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap");
        builder.AddAttribute(56, "rel", "stylesheet");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(57, "link");
        builder.AddAttribute(58, "rel", "stylesheet");
        builder.AddAttribute(59, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(60, "link");
        builder.AddAttribute(61, "rel", "stylesheet");
        builder.AddAttribute(62, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(63, "link");
        builder.AddAttribute(64, "rel", "stylesheet");
        builder.AddAttribute(65, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <script type="module">...</script>
        builder.OpenElement(66, "script");
        builder.AddAttribute(67, "type", "module");
        builder.AddMarkupContent(68, """
            if (!("anchorName" in document.documentElement.style)) {
                import("https://unpkg.com/@oddbird/css-anchor-positioning");
            }
            """);
        builder.CloseElement();
    }

    private void RenderFirstPaintThemeLinks(RenderTreeBuilder builder, int sequence) {
        if (DefaultTheme is NTTheme.Dark) {
            RenderFirstPaintThemeLink(builder, sequence, GetThemeCssFile(NTTheme.Dark), null);
            return;
        }

        if (DefaultTheme is NTTheme.Light) {
            RenderFirstPaintThemeLink(builder, sequence, GetThemeCssFile(NTTheme.Light), null);
            return;
        }

        RenderFirstPaintThemeLink(builder, sequence, GetThemeCssFile(NTTheme.Light), LightThemeMedia);
        RenderFirstPaintThemeLink(builder, sequence + 6, GetThemeCssFile(NTTheme.Dark), DarkThemeMedia);
    }

    private void RenderFirstPaintThemeLink(RenderTreeBuilder builder, int sequence, string cssFile, string? media) {
        builder.OpenElement(sequence, "link");
        builder.AddAttribute(sequence + 1, "rel", "stylesheet");
        builder.AddAttribute(sequence + 2, "href", CreateThemeStylesheetHref(ThemesRoot, cssFile));
        builder.AddAttribute(sequence + 3, "media", media);
        builder.AddAttribute(sequence + 4, "data-nt-theme-default", "true");
        builder.AddAttribute(sequence + 5, "data-permanent", string.Empty);
        builder.CloseElement();
    }

    private string GetThemeCssFile(NTTheme theme) => theme is NTTheme.Dark
        ? DefaultContrast switch {
            NTThemeContrast.Medium => DarkMediumCss,
            NTThemeContrast.High => DarkHighCss,
            _ => DarkDefaultCss
        }
        : DefaultContrast switch {
            NTThemeContrast.Medium => LightMediumCss,
            NTThemeContrast.High => LightHighCss,
            _ => LightDefaultCss
        };

    private NTThemeConfiguration CreateThemeConfiguration() => new() {
        DefaultContrast = DefaultContrast,
        DefaultTheme = DefaultTheme,
        ThemesRoot = ThemesRoot,
        LightDefaultCss = LightDefaultCss,
        LightMediumCss = LightMediumCss,
        LightHighCss = LightHighCss,
        DarkDefaultCss = DarkDefaultCss,
        DarkMediumCss = DarkMediumCss,
        DarkHighCss = DarkHighCss
    };
}
