namespace NTComponents;

/// <summary>
/// Represents the standard elevation levels exposed by NTComponents.
/// </summary>
/// <remarks>
/// <para> Elevation is used to communicate depth, separation, and hierarchy between surfaces. </para> <para> The
/// elevation scale maps to these shadow depths: <see cref="None" /> = 0px, <see cref="Lowest" /> = 1px,
/// <see cref="Low" /> = 3px, <see cref="Medium" /> = 6px, <see cref="High" /> = 8px, and <see cref="Highest" /> = 12px.
/// </para> <para> Keep elevation mapping consistent for the same layout regions and component roles so hierarchy
/// remains predictable across the UI. </para>
/// </remarks>
public enum NTElevation {
    /// <summary>
    /// No elevation. Maps to 0px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this when a surface should sit flush with its surrounding layout and should not appear raised. Do
    /// not use it when the component relies on elevation to separate itself from nearby surfaces. </para>
    /// </remarks>
    None,

    /// <summary>
    /// Lowest elevation. Maps to 1px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for the most subtle raised surfaces when only minimal separation is needed. Do not use it
    /// when the component needs strong visual prominence. </para>
    /// </remarks>
    Lowest,

    /// <summary>
    /// Low elevation. Maps to 3px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for lightly elevated surfaces that should read above the base layout without becoming a
    /// primary focal point. Do not use it when either no separation or strong separation is required. </para>
    /// </remarks>
    Low,

    /// <summary>
    /// Medium elevation. Maps to 6px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for clearly raised surfaces that need noticeable separation from surrounding content. Do not
    /// use it by default for every container, or the elevation hierarchy will flatten. </para>
    /// </remarks>
    Medium,

    /// <summary>
    /// High elevation. Maps to 8px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for strongly elevated surfaces that should stand apart from the rest of the interface. Do not
    /// use it for ordinary resting surfaces that only need subtle depth. </para>
    /// </remarks>
    High,

    /// <summary>
    /// Highest elevation. Maps to 12px.
    /// </summary>
    /// <remarks>
    /// <para> Do use this for the most prominent elevated surfaces when they must appear clearly above all lower
    /// layers. Do not use it broadly across the layout, or more important layers will lose distinction. </para>
    /// </remarks>
    Highest
}

/// <summary>
/// Provides extension methods for <see cref="NTElevation"/>.
/// </summary>
public static class NTElevationExt {
    /// <summary>
    /// Converts the elevation value to its corresponding CSS class name.
    /// </summary>
    /// <param name="elevation">The elevation level to convert.</param>
    /// <returns>The CSS class name corresponding to the elevation level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="elevation"/> is not a valid <see cref="NTElevation"/> value.
    /// </exception>
    public static string ToCssClass(this NTElevation elevation) {
        return elevation switch {
            NTElevation.None => "nt-elevation-none",
            NTElevation.Lowest => "nt-elevation-lowest",
            NTElevation.Low => "nt-elevation-low",
            NTElevation.Medium => "nt-elevation-medium",
            NTElevation.High => "nt-elevation-high",
            NTElevation.Highest => "nt-elevation-highest",
            _ => throw new ArgumentOutOfRangeException(nameof(elevation), elevation, null)
        };
    }
}