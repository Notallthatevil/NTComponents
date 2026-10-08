using System.Text.RegularExpressions;

namespace NTComponents;

/// <summary>
/// Provides CSS conversions for Material theme colors.
/// </summary>
public static partial class NTColorEnumExt {
    /// <summary>Converts a color to its CSS token suffix.</summary>
    public static string ToCssClassName(this NTColor color) => FindCapitals().Replace(color.ToString(), "-$1").ToLowerInvariant();

    /// <summary>Converts an optional color to its CSS token suffix.</summary>
    public static string ToCssClassName(this NTColor? color) => color.HasValue ? color.Value.ToCssClassName() : string.Empty;

    /// <summary>Converts a color to its NT theme variable reference.</summary>
    public static string ToCssNTColorVariable(this NTColor color) => $"var(--nt-color-{color.ToCssClassName()})";

    /// <summary>Converts an optional color to its NT theme variable reference.</summary>
    public static string ToCssNTColorVariable(this NTColor? color) => color.HasValue ? color.Value.ToCssNTColorVariable() : string.Empty;

    [GeneratedRegex(@"(?<=.)([A-Z])")]
    private static partial Regex FindCapitals();
}
