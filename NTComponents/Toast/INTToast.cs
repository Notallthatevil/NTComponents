using NTComponents.Core;

namespace NTComponents.Toast;

/// <summary>
///     Represents a toast notification rendered through the <see cref="NTToast" /> JavaScript host.
/// </summary>
public interface INTToast {
    /// <summary>
    ///     Gets the toast container color.
    /// </summary>
    TnTColor BackgroundColor { get; }

    /// <summary>
    ///     Gets the icon name rendered at the start of the toast, when present.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    ///     Gets the icon color.
    /// </summary>
    TnTColor IconColor { get; }

    /// <summary>
    ///     Gets the supporting message shown below the title.
    /// </summary>
    string? Message { get; }

    /// <summary>
    ///     Gets a value indicating whether the dismiss affordance should be shown.
    /// </summary>
    bool ShowClose { get; }

    /// <summary>
    ///     Gets the text color.
    /// </summary>
    TnTColor TextColor { get; }

    /// <summary>
    ///     Gets the timeout in seconds before auto-dismiss. Zero or less disables auto-dismiss.
    /// </summary>
    double Timeout { get; }

    /// <summary>
    ///     Gets the toast title.
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     Gets the semantic toast variant.
    /// </summary>
    NTToastVariant Variant { get; }
}
