using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NTComponents.Core;

/// <summary>
///     A builder class for constructing CSS style strings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CssStyleBuilder {

    /// <summary>
    ///     Internal dictionary of CSS property name to value pairs collected by the builder.
    /// </summary>
    private readonly Dictionary<string, string> _styles = [];

    /// <summary>
    ///     Holds raw style fragments added via <see cref="Add(string)" />. Initialized to an empty string.
    /// </summary>
    private string? _styleString = string.Empty;

    /// <summary>
    ///     Prevents external instantiation. Use <see cref="Create" /> to obtain an instance.
    /// </summary>
    private CssStyleBuilder() {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="CssStyleBuilder" />.
    /// </summary>
    /// <returns>A new instance of <see cref="CssStyleBuilder" />.</returns>
    public static CssStyleBuilder Create() => new();

    /// <summary>
    ///     Appends a raw CSS style fragment to the builder.
    /// </summary>
    /// <param name="style">The style fragment to append (for example, "margin:4px").</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder Add(string style) {
        if (!string.IsNullOrWhiteSpace(style)) {
            _styleString += style;
            if (!_styleString.EndsWith(';')) {
                _styleString += ';';
            }
        }
        return this;
    }

    /// <summary>
    ///     Adds a <c>background-color</c> style using a theme color variable when provided.
    /// </summary>
    /// <param name="color">  The optional <see cref="TnTColor" /> to use as the background color.</param>
    /// <param name="enabled">If false, the background-color will not be added.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddBackgroundColor(TnTColor? color, bool enabled = true) => enabled && color.HasValue ? AddStyle("background-color", color?.ToCssTnTColorVariable()) : this;

    /// <summary>Adds an NT theme background color.</summary>
    public CssStyleBuilder AddBackgroundColor(NTColor color, bool enabled = true) => AddStyle("background-color", color.ToCssNTColorVariable(), enabled);

    /// <summary>
    ///     Adds a <c>color</c> (foreground) style using a theme color variable when provided.
    /// </summary>
    /// <param name="color">  The optional <see cref="TnTColor" /> to use as the text color.</param>
    /// <param name="enabled">If false, the color will not be added.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddForegroundColor(TnTColor? color, bool enabled = true) => enabled && color.HasValue ? AddStyle("color", color?.ToCssTnTColorVariable()) : this;

    /// <summary>Adds an NT theme foreground color.</summary>
    public CssStyleBuilder AddForegroundColor(NTColor color, bool enabled = true) => AddStyle("color", color.ToCssNTColorVariable(), enabled);

    /// <summary>
    ///     Adds styles from an attributes dictionary if it contains a "style" key.
    /// </summary>
    /// <param name="additionalAttributes">A dictionary of additional attributes that may contain a "style" entry.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddFromAdditionalAttributes(IReadOnlyDictionary<string, object>? additionalAttributes) {
        return additionalAttributes?.TryGetValue("style", out var style) == true && style is not null
            ? AddStyle(style.ToString(), string.Empty)
            : this;
    }

    /// <summary>
    ///     Adds a CSS property (key/value) to the builder.
    /// </summary>
    /// <param name="key">    The CSS property name (for example, "padding").</param>
    /// <param name="value">  The CSS property value (for example, "8px").</param>
    /// <param name="enabled">If false, the property will not be added.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddStyle(string? key, string? value, bool enabled = true) {
        if (!string.IsNullOrWhiteSpace(key) && value is not null && enabled) {
            _styles[key] = value;
        }
        return this;
    }

    /// <summary>
    ///     Adds a custom CSS variable to the builder.
    /// </summary>
    /// <param name="varName"> The name of the CSS variable (without the leading <c>--</c>).</param>
    /// <param name="varValue">The value to assign to the CSS variable.</param>
    /// <param name="enabled"> If false, the variable will not be added.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddVariable(string varName, string varValue, bool enabled = true) => enabled ? AddStyle($"--{varName}", varValue) : this;

    /// <summary>
    ///     Adds a CSS variable referencing a theme color to the builder.
    /// </summary>
    /// <param name="varName">The name of the CSS variable (without the leading <c>--</c>).</param>
    /// <param name="color">  The <see cref="TnTColor" /> value used to construct the variable reference.</param>
    /// <param name="enabled">If false, the variable will not be added.</param>
    /// <returns>The current instance of <see cref="CssStyleBuilder" /> for chaining.</returns>
    public CssStyleBuilder AddVariable(string varName, TnTColor color, bool enabled = true) => enabled ? AddStyle($"--{varName}", $"var(--tnt-color-{color.ToCssClassName()})") : this;

    /// <summary>Adds a custom property referencing an NT theme color.</summary>
    public CssStyleBuilder AddVariable(string varName, NTColor color, bool enabled = true) => AddStyle($"--{varName}", color.ToCssNTColorVariable(), enabled);

    /// <summary>
    ///     Builds the final CSS style string composed of any raw fragments and collected property/value pairs.
    /// </summary>
    /// <returns>A combined CSS style string (for example, "margin:4px;color:var(--tnt-color-primary);") or <c>null</c> if no styles were added.</returns>
    public string? Build() {
        var styles = _styles.Where(kv => !string.IsNullOrWhiteSpace(kv.Key));
        var sb = new StringBuilder(_styleString);
        if (styles.Any()) {
            foreach (var (key, value) in styles) {
                var trimmedKey = key.Trim();
                var trimmedValue = value.Trim();
                sb.Append(trimmedKey);
                if (!string.IsNullOrWhiteSpace(trimmedValue)) {
                    if (!trimmedKey.EndsWith(':')) {
                        sb.Append(':');
                    }
                    sb.Append(trimmedValue);

                    if (!trimmedValue.EndsWith(';')) {
                        sb.Append(';');
                    }
                }
                else {
                    if (!trimmedKey.EndsWith(';')) {
                        sb.Append(';');
                    }
                }
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
