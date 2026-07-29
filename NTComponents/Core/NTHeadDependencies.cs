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
    private const string ActiveThemeElementId = "nt-theme-active-slot";
    private const string PendingThemeElementId = "nt-theme-pending-slot";
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

    internal static void RenderMeasurementTokens(RenderTreeBuilder builder, double headerHeight, double footerHeight, double sideNavWidth, string? tokenScopeSelector) {
        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "class", "tnt-measurements");
        builder.AddContent(2, CreateMeasurementTokens(headerHeight, footerHeight, sideNavWidth, tokenScopeSelector));
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
        builder.AddAttribute(3, "id", "nt-theme-critical");
        builder.AddAttribute(4, "data-permanent", string.Empty);
        builder.AddContent(5, "html, body, #app { background-color: Canvas; color: CanvasText; }");
        builder.CloseElement();

        builder.OpenRegion(6);
        RenderFirstPaintThemeLinks(builder);
        builder.CloseRegion();

        // Stable empty slots are preserved across enhanced navigation and activated by NTThemeRuntime as needed.
        builder.OpenRegion(20);
        RenderThemeSlot(builder, ActiveThemeElementId);
        builder.CloseRegion();
        builder.OpenRegion(23);
        RenderThemeSlot(builder, PendingThemeElementId);
        builder.CloseRegion();

        // <script type="application/json" id="nt-theme-config">...</script>
        builder.OpenElement(26, "script");
        builder.AddAttribute(27, "type", "application/json");
        builder.AddAttribute(28, "id", "nt-theme-config");
        builder.AddAttribute(29, "data-permanent", string.Empty);
        builder.AddMarkupContent(30, CreateThemeConfiguration().ToJson());
        builder.CloseElement();

        // <script type="application/json" id="nt-theme-state">...</script>
        builder.OpenElement(31, "script");
        builder.AddAttribute(32, "type", "application/json");
        builder.AddAttribute(33, "id", "nt-theme-state");
        builder.AddAttribute(34, "data-permanent", string.Empty);
        builder.AddMarkupContent(35, "{}");
        builder.CloseElement();

        // <script src="_content/NTComponents/NTTheme.runtime.js"></script>
        builder.OpenElement(36, "script");
        builder.AddAttribute(37, "id", "nt-theme-runtime-script");
        builder.AddAttribute(38, "src", "_content/NTComponents/NTTheme.runtime.js");
        builder.AddAttribute(39, "data-permanent", string.Empty);
        builder.CloseElement();

        // <script src="_content/NTComponents/theme-bootstrap.js"></script>
        builder.OpenElement(40, "script");
        builder.AddAttribute(41, "id", "nt-theme-bootstrap-script");
        builder.AddAttribute(42, "src", "_content/NTComponents/theme-bootstrap.js");
        builder.AddAttribute(43, "data-permanent", string.Empty);
        builder.CloseElement();

        // <style class="tnt-measurements">...</style>
        builder.OpenRegion(44);
        RenderMeasurementTokens(builder, HeaderHeight, FooterHeight, SideNavWidth, TokenScopeSelector);
        builder.CloseRegion();

        // <link rel="stylesheet" href="_content/NTComponents/nt-measurements.css">
        builder.OpenElement(48, "link");
        builder.AddAttribute(49, "id", "nt-measurements-stylesheet");
        builder.AddAttribute(50, "rel", "stylesheet");
        builder.AddAttribute(51, "href", "_content/NTComponents/nt-measurements.css");
        builder.AddAttribute(52, "data-permanent", string.Empty);
        builder.CloseElement();

        // <link rel="stylesheet" href="_content/NTComponents/nt-ripple.css">
        builder.OpenElement(53, "link");
        builder.AddAttribute(54, "rel", "stylesheet");
        builder.AddAttribute(55, "href", "_content/NTComponents/nt-ripple.css");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.googleapis.com">
        builder.OpenElement(56, "link");
        builder.AddAttribute(57, "rel", "preconnect");
        builder.AddAttribute(58, "href", "https://fonts.googleapis.com");
        builder.CloseElement();

        // <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        builder.OpenElement(59, "link");
        builder.AddAttribute(60, "rel", "preconnect");
        builder.AddAttribute(61, "href", "https://fonts.gstatic.com");
        builder.AddAttribute(62, "crossorigin", string.Empty);
        builder.CloseElement();

        // <link href="https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap" rel="stylesheet">
        builder.OpenElement(63, "link");
        builder.AddAttribute(64, "href", "https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap");
        builder.AddAttribute(65, "rel", "stylesheet");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(66, "link");
        builder.AddAttribute(67, "rel", "stylesheet");
        builder.AddAttribute(68, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(69, "link");
        builder.AddAttribute(70, "rel", "stylesheet");
        builder.AddAttribute(71, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200" />
        builder.OpenElement(72, "link");
        builder.AddAttribute(73, "rel", "stylesheet");
        builder.AddAttribute(74, "href", "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200");
        builder.CloseElement();

        // <script type="module">...</script>
        builder.OpenElement(75, "script");
        builder.AddAttribute(76, "type", "module");
        builder.AddMarkupContent(77, """
            if (!("anchorName" in document.documentElement.style)) {
                import("https://unpkg.com/@oddbird/css-anchor-positioning");
            }
            """);
        builder.CloseElement();
    }

    private void RenderFirstPaintThemeLinks(RenderTreeBuilder builder) {
        if (DefaultTheme is NTTheme.Dark) {
            builder.OpenRegion(0);
            RenderFirstPaintThemeLink(builder, NTTheme.Dark, GetThemeCssFile(NTTheme.Dark), null);
            builder.CloseRegion();
            return;
        }

        if (DefaultTheme is NTTheme.Light) {
            builder.OpenRegion(0);
            RenderFirstPaintThemeLink(builder, NTTheme.Light, GetThemeCssFile(NTTheme.Light), null);
            builder.CloseRegion();
            return;
        }

        builder.OpenRegion(0);
        RenderFirstPaintThemeLink(builder, NTTheme.Light, GetThemeCssFile(NTTheme.Light), LightThemeMedia);
        builder.CloseRegion();
        builder.OpenRegion(1);
        RenderFirstPaintThemeLink(builder, NTTheme.Dark, GetThemeCssFile(NTTheme.Dark), DarkThemeMedia);
        builder.CloseRegion();
    }

    private void RenderFirstPaintThemeLink(RenderTreeBuilder builder, NTTheme theme, string cssFile, string? media) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", theme is NTTheme.Dark ? "nt-theme-default-dark" : "nt-theme-default-light");
        builder.AddAttribute(2, "rel", "stylesheet");
        builder.AddAttribute(3, "href", CreateThemeStylesheetHref(ThemesRoot, cssFile));
        builder.AddAttribute(4, "media", media);
        builder.AddAttribute(5, "data-nt-theme-default", "true");
        builder.AddAttribute(6, "data-permanent", string.Empty);
        builder.CloseElement();
    }

    private static void RenderThemeSlot(RenderTreeBuilder builder, string id) {
        builder.OpenElement(0, "link");
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "data-permanent", string.Empty);
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
