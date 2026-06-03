namespace NTComponents;

/// <summary>
///     Stores the inclusive start and end values selected by an <see cref="NTInputRangeSlider{TNumber}" />.
/// </summary>
/// <typeparam name="TNumber">The numeric value type.</typeparam>
public struct NTSliderRange<TNumber> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSliderRange{TNumber}" /> struct.
    /// </summary>
    /// <param name="start">The lower selected value.</param>
    /// <param name="end">The upper selected value.</param>
    public NTSliderRange(TNumber start, TNumber end) {
        Start = start;
        End = end;
    }

    /// <summary>
    ///     Gets or sets the lower selected value.
    /// </summary>
    public TNumber Start { get; set; }

    /// <summary>
    ///     Gets or sets the upper selected value.
    /// </summary>
    public TNumber End { get; set; }
}

/// <summary>
///     Defines the visual variant for a single-value Material 3 slider.
/// </summary>
public enum NTSliderVariant {
    /// <summary>
    ///     The active track starts at the minimum value and ends at the handle.
    /// </summary>
    Standard,

    /// <summary>
    ///     The active track starts at the center value and expands toward the handle.
    /// </summary>
    Centered
}

/// <summary>
///     Defines slider orientation.
/// </summary>
public enum NTSliderOrientation {
    /// <summary>
    ///     Renders the slider along the inline axis.
    /// </summary>
    Horizontal,

    /// <summary>
    ///     Renders the slider along the block axis.
    /// </summary>
    Vertical
}

/// <summary>
///     Defines Material 3 slider size token presets.
/// </summary>
public enum NTSliderSize {
    /// <summary>
    ///     Extra-small slider, matching the default Material 3 16dp track.
    /// </summary>
    ExtraSmall,

    /// <summary>
    ///     Small slider, matching the Material 3 24dp track.
    /// </summary>
    Small,

    /// <summary>
    ///     Medium slider, matching the Material 3 40dp track.
    /// </summary>
    Medium,

    /// <summary>
    ///     Large slider, matching the Material 3 56dp track.
    /// </summary>
    Large,

    /// <summary>
    ///     Extra-large slider, matching the Material 3 96dp track.
    /// </summary>
    ExtraLarge
}
