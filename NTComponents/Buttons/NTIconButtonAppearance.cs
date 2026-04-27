namespace NTComponents;

/// <summary>
///     Defines the supported width treatments for <see cref="NTIconButton" />.
/// </summary>
/// <remarks>
///     Use <see cref="Default" /> for most icon buttons. Choose <see cref="Narrow" /> only in dense repeated controls where the action remains easy to target, and use <see cref="Wide" /> when a
///     larger horizontal target improves scanability without requiring a visible text label.
/// </remarks>
public enum NTIconButtonAppearance {

    /// <summary>
    ///     Compact width for dense toolbar or repeated-control layouts.
    /// </summary>
    Narrow,

    /// <summary>
    ///     Default width for most icon-only actions.
    /// </summary>
    Default,

    /// <summary>
    ///     Expanded width for icon-only actions that need a larger horizontal target.
    /// </summary>
    Wide
}
