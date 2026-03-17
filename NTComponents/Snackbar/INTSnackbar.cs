using NTComponents.Core;

namespace NTComponents.Snackbar;

/// <summary>
///     Represents a snackbar notification with Material 3 inspired defaults.
/// </summary>
public interface INTSnackbar {

    /// <summary>
    ///     Gets the action label, when an action is available.
    /// </summary>
    string? ActionLabel { get; }

    /// <summary>
    ///     Gets the action text color.
    /// </summary>
    TnTColor ActionColor { get; }

    /// <summary>
    ///     Gets the snackbar container background color.
    /// </summary>
    TnTColor BackgroundColor { get; }

    /// <summary>
    ///     Gets a value indicating whether the snackbar is currently closing.
    /// </summary>
    bool Closing { get; }

    /// <summary>
    ///     Gets a value indicating whether the snackbar exposes an action button.
    /// </summary>
    bool HasAction { get; }

    /// <summary>
    ///     Gets or sets the supporting text shown in the snackbar.
    /// </summary>
    string Message { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the dismiss affordance should be shown.
    /// </summary>
    bool ShowClose { get; set; }

    /// <summary>
    ///     Gets or sets the timeout in seconds before auto-dismiss. Zero or less disables auto-dismiss.
    /// </summary>
    double Timeout { get; set; }

    /// <summary>
    ///     Gets the supporting text color.
    /// </summary>
    TnTColor TextColor { get; }
}
