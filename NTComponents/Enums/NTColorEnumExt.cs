using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace NTComponents;

/// <summary>
/// Provides CSS conversions for Material theme colors.
/// </summary>
public static partial class NTColorEnumExt {
    private static readonly ConcurrentDictionary<NTColor, string> _cssClassNames = new();
    private static readonly ConcurrentDictionary<NTColor, string> _cssVariables = new();

    /// <summary>Converts a color to its CSS token suffix.</summary>
    public static string ToCssClassName(this NTColor color) => _cssClassNames.GetOrAdd(color, static value => FindCapitals().Replace(value.ToString(), "-$1").ToLowerInvariant());

    /// <summary>Converts an optional color to its CSS token suffix.</summary>
    public static string ToCssClassName(this NTColor? color) => color.HasValue ? color.Value.ToCssClassName() : string.Empty;

    /// <summary>Converts a color to its NT theme variable reference.</summary>
    public static string ToCssNTColorVariable(this NTColor color) => _cssVariables.GetOrAdd(color, static value => $"var(--nt-color-{value.ToCssClassName()})");

    /// <summary>Converts an optional color to its NT theme variable reference.</summary>
    public static string ToCssNTColorVariable(this NTColor? color) => color.HasValue ? color.Value.ToCssNTColorVariable() : string.Empty;

    [GeneratedRegex(@"(?<=.)([A-Z])")]
    private static partial Regex FindCapitals();
}
