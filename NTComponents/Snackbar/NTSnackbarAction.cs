namespace NTComponents;

/// <summary>
///     Represents an action button shown by an <see cref="NTSnackbar" />.
/// </summary>
public sealed class NTSnackbarAction {

    /// <summary>
    ///     Creates a snackbar action.
    /// </summary>
    /// <param name="label">The action button label.</param>
    /// <param name="callback">The callback to invoke. Return <c>true</c> to close the snackbar, or <c>false</c> to keep it open.</param>
    public NTSnackbarAction(string label, Func<Task<bool>> callback) {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(callback);

        Label = label;
        Callback = callback;
    }

    /// <summary>
    ///     Gets the action button label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    ///     Gets the callback to invoke. Return <c>true</c> to close the snackbar, or <c>false</c> to keep it open.
    /// </summary>
    public Func<Task<bool>> Callback { get; }
}
