using System.Text;
using System.Text.Encodings.Web;

namespace NTComponents;

/// <summary>
///     Provides the preferred NTComponents theme runtime configuration.
/// </summary>
public sealed class NTThemeConfiguration {
    private static readonly JavaScriptEncoder _jsonEncoder = JavaScriptEncoder.Default;

    /// <summary>
    ///     Gets or sets the localStorage key used for persisted theme mode.
    /// </summary>
    public string ThemeStorageKey { get; set; } = "NTComponentsStoredThemeKey";

    /// <summary>
    ///     Gets or sets the localStorage key used for persisted contrast mode.
    /// </summary>
    public string ContrastStorageKey { get; set; } = "NTComponentsStoredContrastKey";

    /// <summary>
    ///     Gets or sets the default theme mode when storage has no valid value.
    /// </summary>
    public NTTheme DefaultTheme { get; set; } = NTTheme.System;

    /// <summary>
    ///     Gets or sets the default contrast level when storage has no valid value.
    /// </summary>
    public NTThemeContrast DefaultContrast { get; set; } = NTThemeContrast.Default;

    /// <summary>
    ///     Gets or sets the root path for theme CSS files.
    /// </summary>
    public string ThemesRoot { get; set; } = "/Themes";

    /// <summary>
    ///     Gets or sets the CSS file for the default light theme.
    /// </summary>
    public string LightDefaultCss { get; set; } = "light.css";

    /// <summary>
    ///     Gets or sets the CSS file for the medium contrast light theme.
    /// </summary>
    public string LightMediumCss { get; set; } = "light-mc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the high contrast light theme.
    /// </summary>
    public string LightHighCss { get; set; } = "light-hc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the default dark theme.
    /// </summary>
    public string DarkDefaultCss { get; set; } = "dark.css";

    /// <summary>
    ///     Gets or sets the CSS file for the medium contrast dark theme.
    /// </summary>
    public string DarkMediumCss { get; set; } = "dark-mc.css";

    /// <summary>
    ///     Gets or sets the CSS file for the high contrast dark theme.
    /// </summary>
    public string DarkHighCss { get; set; } = "dark-hc.css";

    internal string ToJson()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        AppendJsonProperty(builder, "themeStorageKey", ThemeStorageKey);
        AppendJsonProperty(builder, "contrastStorageKey", ContrastStorageKey);
        AppendJsonProperty(builder, "defaultTheme", ToRuntimeValue(DefaultTheme));
        AppendJsonProperty(builder, "defaultContrast", ToRuntimeValue(DefaultContrast));
        AppendJsonProperty(builder, "themesRoot", ThemesRoot);
        AppendJsonProperty(builder, "lightDefaultCss", LightDefaultCss);
        AppendJsonProperty(builder, "lightMediumCss", LightMediumCss);
        AppendJsonProperty(builder, "lightHighCss", LightHighCss);
        AppendJsonProperty(builder, "darkDefaultCss", DarkDefaultCss);
        AppendJsonProperty(builder, "darkMediumCss", DarkMediumCss);
        AppendJsonProperty(builder, "darkHighCss", DarkHighCss, false);
        builder.Append('}');
        return builder.ToString();
    }

    internal static string ToRuntimeValue(NTTheme theme) => theme switch {
        NTTheme.Light => "LIGHT",
        NTTheme.Dark => "DARK",
        _ => "SYSTEM"
    };

    internal static string ToRuntimeValue(NTThemeContrast contrast) => contrast switch {
        NTThemeContrast.Medium => "MEDIUM",
        NTThemeContrast.High => "HIGH",
        _ => "DEFAULT"
    };

    internal static NTTheme FromLegacyTheme(Theme theme) => theme switch {
        Theme.Light => NTTheme.Light,
        Theme.Dark => NTTheme.Dark,
        _ => NTTheme.System
    };

    private static void AppendJsonProperty(StringBuilder builder, string name, string? value, bool appendComma = true)
    {
        builder.Append('"').Append(name).Append("\":\"");
        builder.Append(_jsonEncoder.Encode(value ?? string.Empty));
        builder.Append('"');

        if (appendComma) {
            builder.Append(',');
        }
    }
}
