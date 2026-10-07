using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
/// Specifies the direction of the layout.
/// </summary>
[Obsolete("Use NTLayoutDirection instead.")]
public enum LayoutDirection {
    /// <summary>
    /// Layout elements vertically.
    /// </summary>
    Vertical,
    /// <summary>
    /// Layout elements horizontally.
    /// </summary>
    Horizontal
}

/// <summary>
/// Provides extension methods for the <see cref="LayoutDirection"/> enum.
/// </summary>
[ExcludeFromCodeCoverage]
public static class LayoutDirectionExt {
    /// <summary>
    /// Converts the <see cref="LayoutDirection"/> value to its corresponding CSS string representation.
    /// </summary>
    /// <param name="direction">The <see cref="LayoutDirection"/> value to convert.</param>
    /// <returns>A string representing the CSS value for the specified <see cref="LayoutDirection"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the <paramref name="direction"/> is not a valid <see cref="LayoutDirection"/> value.</exception>
    public static string ToCssString(this LayoutDirection direction) {
        return direction switch {
            LayoutDirection.Vertical => "vertical",
            LayoutDirection.Horizontal => "horizontal",
            _ => throw new InvalidOperationException($"{direction} is not a valid value of {nameof(LayoutDirection)}")
        };
    }
}
