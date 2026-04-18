using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NTComponents.Site.Theming;

public sealed class MaterialThemeCssConverter {

    private static readonly ThemeOutputRequest[] _outputRequests = [
        new("light.css", ThemeVariant.Light, ThemeContrast.Default, ["light"]),
        new("light-mc.css", ThemeVariant.Light, ThemeContrast.Medium, ["light-medium-contrast", "lightMediumContrast", "lightMedium", "light-medium", "light_mc", "lightMc"]),
        new("light-hc.css", ThemeVariant.Light, ThemeContrast.High, ["light-high-contrast", "lightHighContrast", "lightHigh", "light-high", "light_hc", "lightHc"]),
        new("dark.css", ThemeVariant.Dark, ThemeContrast.Default, ["dark"]),
        new("dark-mc.css", ThemeVariant.Dark, ThemeContrast.Medium, ["dark-medium-contrast", "darkMediumContrast", "darkMedium", "dark-medium", "dark_mc", "darkMc"]),
        new("dark-hc.css", ThemeVariant.Dark, ThemeContrast.High, ["dark-high-contrast", "darkHighContrast", "darkHigh", "dark-high", "dark_hc", "darkHc"])
    ];

    private static readonly ThemeColorRole[] _themeColorRoles = [
        new("primary", "primary", true),
        new("surfaceTint", "surface-tint", true),
        new("onPrimary", "on-primary", true),
        new("primaryContainer", "primary-container", true),
        new("onPrimaryContainer", "on-primary-container", true),
        new("secondary", "secondary", true),
        new("onSecondary", "on-secondary", true),
        new("secondaryContainer", "secondary-container", true),
        new("onSecondaryContainer", "on-secondary-container", true),
        new("tertiary", "tertiary", true),
        new("onTertiary", "on-tertiary", true),
        new("tertiaryContainer", "tertiary-container", true),
        new("onTertiaryContainer", "on-tertiary-container", true),
        new("error", "error", true),
        new("onError", "on-error", true),
        new("errorContainer", "error-container", true),
        new("onErrorContainer", "on-error-container", true),
        new("background", "background", true),
        new("onBackground", "on-background", true),
        new("surface", "surface", true),
        new("onSurface", "on-surface", true),
        new("surfaceVariant", "surface-variant", true),
        new("onSurfaceVariant", "on-surface-variant", true),
        new("outline", "outline", true),
        new("outlineVariant", "outline-variant", true),
        new("shadow", "shadow", true),
        new("scrim", "scrim", true),
        new("inverseSurface", "inverse-surface", true),
        new("inverseOnSurface", "inverse-on-surface", true),
        new("inversePrimary", "inverse-primary", true),
        new("primaryFixed", "primary-fixed", true),
        new("onPrimaryFixed", "on-primary-fixed", true),
        new("primaryFixedDim", "primary-fixed-dim", true),
        new("onPrimaryFixedVariant", "on-primary-fixed-variant", true),
        new("secondaryFixed", "secondary-fixed", true),
        new("onSecondaryFixed", "on-secondary-fixed", true),
        new("secondaryFixedDim", "secondary-fixed-dim", true),
        new("onSecondaryFixedVariant", "on-secondary-fixed-variant", true),
        new("tertiaryFixed", "tertiary-fixed", true),
        new("onTertiaryFixed", "on-tertiary-fixed", true),
        new("tertiaryFixedDim", "tertiary-fixed-dim", true),
        new("onTertiaryFixedVariant", "on-tertiary-fixed-variant", true),
        new("surfaceDim", "surface-dim", true),
        new("surfaceBright", "surface-bright", true),
        new("surfaceContainerLowest", "surface-container-lowest", true),
        new("surfaceContainerLow", "surface-container-low", true),
        new("surfaceContainer", "surface-container", true),
        new("surfaceContainerHigh", "surface-container-high", true),
        new("surfaceContainerHighest", "surface-container-highest", true),
        new("success", "success", false),
        new("onSuccess", "on-success", false),
        new("successContainer", "success-container", false),
        new("onSuccessContainer", "on-success-container", false),
        new("info", "info", false),
        new("onInfo", "on-info", false),
        new("infoContainer", "info-container", false),
        new("onInfoContainer", "on-info-container", false),
        new("warning", "warning", false),
        new("onWarning", "on-warning", false),
        new("warningContainer", "warning-container", false),
        new("onWarningContainer", "on-warning-container", false),
        new("assert", "assert", false),
        new("onAssert", "on-assert", false),
        new("assertContainer", "assert-container", false),
        new("onAssertContainer", "on-assert-container", false)
    ];

    public ThemeConversionResult Convert(string? materialThemeBuilderJson) {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(materialThemeBuilderJson)) {
            return new ThemeConversionResult([], ["Theme JSON is required."], []);
        }

        using JsonDocument document = ParseDocument(materialThemeBuilderJson, errors);
        if (errors.Count > 0) {
            return new ThemeConversionResult([], errors, warnings);
        }

        var root = document.RootElement;
        if (!TryGetObjectProperty(root, "schemes", out var schemes)) {
            return new ThemeConversionResult([], ["Theme JSON must contain a 'schemes' object."], warnings);
        }

        var outputs = new List<ThemeCssOutput>();
        var defaultSchemes = new Dictionary<ThemeVariant, JsonElement>();

        foreach (var request in _outputRequests) {
            if (!TryGetScheme(schemes, request.SchemeKeys, out var scheme)) {
                if (request.Contrast == ThemeContrast.Default) {
                    errors.Add($"Theme JSON is missing the required '{request.SchemeKeys[0]}' scheme.");
                    continue;
                }

                if (defaultSchemes.TryGetValue(request.Variant, out var defaultScheme)) {
                    warnings.Add($"Theme JSON does not include a {request.Variant.ToString().ToLowerInvariant()} {request.Contrast.ToString().ToLowerInvariant()} contrast scheme. '{request.FileName}' was generated from the default {request.Variant.ToString().ToLowerInvariant()} scheme.");
                    outputs.Add(ConvertScheme(request, defaultScheme, errors, warnings));
                }
                else {
                    warnings.Add($"Theme JSON does not include a {request.Variant.ToString().ToLowerInvariant()} {request.Contrast.ToString().ToLowerInvariant()} contrast scheme. '{request.FileName}' could not be generated because the default {request.Variant.ToString().ToLowerInvariant()} scheme is missing.");
                }

                continue;
            }

            if (request.Contrast == ThemeContrast.Default) {
                defaultSchemes[request.Variant] = scheme;
            }

            outputs.Add(ConvertScheme(request, scheme, errors, warnings));
        }

        if (errors.Count > 0) {
            return new ThemeConversionResult([], errors, warnings);
        }

        return new ThemeConversionResult(outputs, errors, warnings);
    }

    private static JsonDocument ParseDocument(string json, List<string> errors) {
        try {
            return JsonDocument.Parse(json, new JsonDocumentOptions {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
        }
        catch (JsonException ex) {
            errors.Add($"Theme JSON is invalid: {ex.Message}");
            return JsonDocument.Parse("{}");
        }
    }

    private static ThemeCssOutput ConvertScheme(ThemeOutputRequest request, JsonElement scheme, List<string> errors, List<string> warnings) {
        var css = new StringBuilder();

        css.AppendLine(":root {");

        foreach (var role in _themeColorRoles) {
            if (!TryGetStringProperty(scheme, role.JsonName, out var colorText)) {
                if (role.Required) {
                    errors.Add($"Scheme '{request.SchemeKeys[0]}' is missing required color '{role.JsonName}'.");
                }
                else {
                    warnings.Add($"Scheme '{request.SchemeKeys[0]}' does not include optional NTComponents color '{role.JsonName}'.");
                }

                continue;
            }

            if (!TryConvertToRgb(colorText, out var rgb)) {
                errors.Add($"Scheme '{request.SchemeKeys[0]}' color '{role.JsonName}' has unsupported value '{colorText}'. Use #RGB, #RRGGBB, or rgb(r g b).");
                continue;
            }

            css.Append(CultureInfo.InvariantCulture, $"    --tnt-color-{role.CssName}: rgb({rgb.Red} {rgb.Green} {rgb.Blue});");
            css.AppendLine();
        }

        css.AppendLine("}");

        return new ThemeCssOutput(request.FileName, request.Variant, request.Contrast, css.ToString());
    }

    private static bool TryGetScheme(JsonElement schemes, IReadOnlyList<string> schemeKeys, out JsonElement scheme) {
        foreach (var schemeKey in schemeKeys) {
            if (TryGetObjectProperty(schemes, schemeKey, out scheme)) {
                return true;
            }
        }

        scheme = default;
        return false;
    }

    private static bool TryGetObjectProperty(JsonElement element, string propertyName, out JsonElement property) =>
        TryGetProperty(element, propertyName, out property) && property.ValueKind == JsonValueKind.Object;

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value) {
        if (TryGetProperty(element, propertyName, out var property) && property.ValueKind == JsonValueKind.String) {
            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property) {
        if (element.ValueKind != JsonValueKind.Object) {
            property = default;
            return false;
        }

        foreach (var candidate in element.EnumerateObject()) {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase)) {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static bool TryConvertToRgb(string colorText, out RgbColor rgb) {
        colorText = colorText.Trim();

        if (TryConvertHexToRgb(colorText, out rgb)) {
            return true;
        }

        return TryConvertCssRgbToRgb(colorText, out rgb);
    }

    private static bool TryConvertHexToRgb(string colorText, out RgbColor rgb) {
        rgb = default;

        if (!colorText.StartsWith('#')) {
            return false;
        }

        var hex = colorText[1..];
        if (hex.Length == 3) {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }
        else if (hex.Length != 6) {
            return false;
        }

        if (!int.TryParse(hex[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            || !int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            || !int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue)) {
            return false;
        }

        rgb = new RgbColor(red, green, blue);
        return true;
    }

    private static bool TryConvertCssRgbToRgb(string colorText, out RgbColor rgb) {
        rgb = default;

        if (!colorText.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) || !colorText.EndsWith(')')) {
            return false;
        }

        var inner = colorText[4..^1].Trim();
        var parts = inner.Contains(',') ? inner.Split(',', StringSplitOptions.TrimEntries) : inner.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3) {
            return false;
        }

        if (!TryConvertColorChannel(parts[0], out var red)
            || !TryConvertColorChannel(parts[1], out var green)
            || !TryConvertColorChannel(parts[2], out var blue)) {
            return false;
        }

        rgb = new RgbColor(red, green, blue);
        return true;
    }

    private static bool TryConvertColorChannel(string value, out int channel) {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out channel)) {
            return channel is >= 0 and <= 255;
        }

        channel = default;
        return false;
    }

    private sealed record ThemeOutputRequest(
        string FileName,
        ThemeVariant Variant,
        ThemeContrast Contrast,
        IReadOnlyList<string> SchemeKeys);

    private sealed record ThemeColorRole(string JsonName, string CssName, bool Required);

    private readonly record struct RgbColor(int Red, int Green, int Blue);
}
