using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents;

/// <summary>
///     Represents different sizes that can be used in the application.
/// </summary>
public enum Size {

    /// <summary>
    ///     The smallest size.
    /// </summary>
    Smallest,

    /// <summary>
    ///     Alias for the smallest size.
    /// </summary>
    XS = Smallest,

    /// <summary>
    ///     A small size.
    /// </summary>
    Small,

    /// <summary>
    ///     A medium size.
    /// </summary>
    Medium,

    /// <summary>
    ///     A large size.
    /// </summary>
    Large,

    /// <summary>
    ///     The largest size.
    /// </summary>
    Largest,

    /// <summary>
    ///     Alias for the largest size.
    /// </summary>
    XL = Largest
}

/// <summary>
/// Extension methods for the <see cref="Size"/> enum, providing functionality to convert sizes to CSS class names.
/// </summary>
public static class SizeExtensions {
    /// <summary>
    /// Converts the size to a CSS class name.
    /// </summary>
    /// <param name="size">The size to convert.</param>
    /// <returns>A CSS class name corresponding to the size.</returns>
    public static string ToCssClass(this Size size) {
        return size switch {
            Size.Smallest => "nt-size-xs",
            Size.Small => "nt-size-s",
            Size.Medium => "nt-size-m",
            Size.Large => "nt-size-l",
            Size.Largest => "nt-size-xl",
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }
}