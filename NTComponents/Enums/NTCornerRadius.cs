namespace NTComponents;

/// <summary>
///     Represents the Material 3 corner radius scale exposed by NTComponents.
/// </summary>
/// <remarks>
///     <para>The Material 3 corner radius scale defines ten named roundedness steps for rectangular surfaces and controls.</para>
///     <para>
///         These values come from the Material 3 corner radius scale: <see cref="None" /> = 0dp, <see cref="ExtraSmall" /> = 4dp, <see cref="Small" /> = 8dp, <see cref="Medium" /> = 12dp, <see
///         cref="Large" /> = 16dp, <see cref="LargeIncreased" /> = 20dp, <see cref="ExtraLarge" /> = 28dp, <see cref="ExtraLargeIncreased" /> = 32dp, <see cref="ExtraExtraLarge" /> = 48dp, and <see
///         cref="Full" /> = fully rounded corners.
///     </para>
///     <para>
///         Use <see cref="NTCornerRadiusExt.ToCssValue(NTCornerRadius)" /> when converting these values to CSS. The <see cref="Full" /> token maps to <c>50%</c> instead of a fixed dp value so the
///         shape becomes fully rounded.
///     </para>
///     <para>Best practice: keep component families on a small subset of the scale so roundedness feels intentional and consistent across the UI.</para>
/// </remarks>
public enum NTCornerRadius {

    /// <summary>
    ///     No corner rounding. Maps to 0dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for square surfaces or layouts where sharp edges are part of the visual language. Do not use it when a component is meant to feel soft or approachable.</para>
    /// </remarks>
    None,

    /// <summary>
    ///     Extra small corner rounding. Maps to 4dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for subtle rounding on compact controls and low-emphasis surfaces. Do not use it when the design needs visibly rounded corners at a glance.</para>
    /// </remarks>
    ExtraSmall,

    /// <summary>
    ///     Small corner rounding. Maps to 8dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for lightly rounded controls and grouped containers. Do not use it as the only roundedness option everywhere if the UI needs stronger shape hierarchy.</para>
    /// </remarks>
    Small,

    /// <summary>
    ///     Medium corner rounding. Maps to 12dp.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for standard rounded surfaces that should feel clearly softened without becoming highly expressive. Do not use it for components that are intentionally square or fully pill-shaped.
    ///     </para>
    /// </remarks>
    Medium,

    /// <summary>
    ///     Large corner rounding. Maps to 16dp.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this when a surface should feel obviously rounded, such as prominent containers or larger controls. Do not use it on dense components if the extra clipping area harms content fit.
    ///     </para>
    /// </remarks>
    Large,

    /// <summary>
    ///     Large increased corner rounding. Maps to 20dp.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for surfaces that should feel softer than the standard large step without reaching the extra-large family. Do not use it by default if the product already relies on <see
    ///         cref="Large" /> for large surfaces.
    ///     </para>
    /// </remarks>
    LargeIncreased,

    /// <summary>
    ///     Extra large corner rounding. Maps to 28dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for highly rounded surfaces and more expressive hero containers. Do not use it on text-dense components unless you have allowed enough padding and optical balance.</para>
    /// </remarks>
    ExtraLarge,

    /// <summary>
    ///     Extra large increased corner rounding. Maps to 32dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this when a surface should feel very soft and intentionally rounded. Do not use it broadly across every container or the system will lose shape contrast.</para>
    /// </remarks>
    ExtraLargeIncreased,

    /// <summary>
    ///     Extra extra large corner rounding. Maps to 48dp.
    /// </summary>
    /// <remarks>
    ///     <para>Do use this for very round, expressive surfaces and hero moments. Do not use it where the resulting corner clipping would crowd content or imagery.</para>
    /// </remarks>
    ExtraExtraLarge,

    /// <summary>
    ///     Fully rounded corners.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Do use this for pill, capsule, circular, or fully rounded treatments. Do not treat it as a fixed dp radius; in CSS it should map to <c>50%</c> so the element becomes fully rounded
    ///         relative to its size.
    ///     </para>
    /// </remarks>
    Full
}

/// <summary>
///     Provides extension methods for <see cref="NTCornerRadius" />.
/// </summary>
public static class NTCornerRadiusExt {
    /// <summary>
    ///     Converts the corner radius token to its corresponding CSS utility class.
    /// </summary>
    /// <param name="cornerRadius">The corner radius token to convert.</param>
    /// <returns>The CSS utility class for the token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cornerRadius" /> is not a valid <see cref="NTCornerRadius" /> value.</exception>
    public static string ToCssClass(this NTCornerRadius cornerRadius) {
        return cornerRadius switch {
            NTCornerRadius.None => "nt-corner-radius-none",
            NTCornerRadius.ExtraSmall => "nt-corner-radius-extra-small",
            NTCornerRadius.Small => "nt-corner-radius-small",
            NTCornerRadius.Medium => "nt-corner-radius-medium",
            NTCornerRadius.Large => "nt-corner-radius-large",
            NTCornerRadius.LargeIncreased => "nt-corner-radius-large-increased",
            NTCornerRadius.ExtraLarge => "nt-corner-radius-extra-large",
            NTCornerRadius.ExtraLargeIncreased => "nt-corner-radius-extra-large-increased",
            NTCornerRadius.ExtraExtraLarge => "nt-corner-radius-extra-extra-large",
            NTCornerRadius.Full => "nt-corner-radius-full",
            _ => throw new ArgumentOutOfRangeException(nameof(cornerRadius), cornerRadius, null)
        };
    }


    /// <summary>
    ///     Converts the corner radius token to its corresponding CSS value.
    /// </summary>
    /// <param name="cornerRadius">The corner radius token to convert.</param>
    /// <returns>The CSS border radius value for the token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cornerRadius" /> is not a valid <see cref="NTCornerRadius" /> value.</exception>
    public static string ToCssValue(this NTCornerRadius cornerRadius) {
        return cornerRadius switch {
            NTCornerRadius.None => "0px",
            NTCornerRadius.ExtraSmall => "4px",
            NTCornerRadius.Small => "8px",
            NTCornerRadius.Medium => "12px",
            NTCornerRadius.Large => "16px",
            NTCornerRadius.LargeIncreased => "20px",
            NTCornerRadius.ExtraLarge => "28px",
            NTCornerRadius.ExtraLargeIncreased => "32px",
            NTCornerRadius.ExtraExtraLarge => "48px",
            NTCornerRadius.Full => "50%",
            _ => throw new ArgumentOutOfRangeException(nameof(cornerRadius), cornerRadius, null)
        };
    }
}
