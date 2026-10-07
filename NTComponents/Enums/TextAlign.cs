using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Specifies the text alignment options.
/// </summary>
[Obsolete("Use NTTextAlign instead.")]
public enum TextAlign {

    /// <summary>
    ///     Align text to the left.
    /// </summary>
    Left,

    /// <summary>
    ///     Align text to the center.
    /// </summary>
    Center,

    /// <summary>
    ///     Align text to the right.
    /// </summary>
    Right,

    /// <summary>
    ///     Justify the text.
    /// </summary>
    Justify
}

/// <summary>
///     Provides extension methods for the <see cref="TextAlign" /> enum.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TextAlignExtensions {

    /// <summary>
    ///     Converts the <see cref="TextAlign" /> value to its corresponding CSS string representation.
    /// </summary>
    /// <param name="textAlign">The <see cref="TextAlign" /> value to convert.</param>
    /// <returns>A string representing the CSS text-align value.</returns>
    public static string ToCssString(this TextAlign? textAlign) {
        return textAlign switch {
            TextAlign.Left => "left",
            TextAlign.Center => "center",
            TextAlign.Right => "right",
            TextAlign.Justify => "justify",
            _ => string.Empty
        };
    }
}