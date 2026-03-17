using NTComponents.Core;
using NTComponents.Snackbar;

namespace NTComponents;

/// <summary>
///     Service contract for showing, dismissing, and interacting with snackbars.
/// </summary>
public interface INTSnackbarService
{

    /// <summary>
    ///     Event triggered when a snackbar closes.
    /// </summary>
    public event OnCloseCallback? OnClose;

    /// <summary>
    ///     Delegate for the close event.
    /// </summary>
    /// <param name="snackbar">The snackbar being closed.</param>
    public delegate Task OnCloseCallback(INTSnackbar snackbar);

    /// <summary>
    ///     Event triggered when a snackbar opens.
    /// </summary>
    public event OnOpenCallback? OnOpen;

    /// <summary>
    ///     Delegate for the open event.
    /// </summary>
    /// <param name="snackbar">The snackbar being opened.</param>
    public delegate Task OnOpenCallback(INTSnackbar snackbar);

    /// <summary>
    ///     Closes the supplied snackbar.
    /// </summary>
    /// <param name="snackbar">The snackbar to close.</param>
    Task CloseAsync(INTSnackbar snackbar);

    /// <summary>
    ///     Executes the snackbar action callback, when present, and closes the snackbar on success.
    /// </summary>
    /// <param name="snackbar">The snackbar whose action should run.</param>
    Task InvokeActionAsync(INTSnackbar snackbar);

    /// <summary>
    ///     Shows a snackbar. By default, snackbars without actions auto-dismiss after 6 seconds, while
    ///     action snackbars remain visible until acted on or dismissed.
    /// </summary>
    /// <param name="message">The snackbar supporting text.</param>
    /// <param name="actionLabel">Optional action label. Must be provided with <paramref name="actionCallback" />.</param>
    /// <param name="actionCallback">Optional action callback. Must be provided with <paramref name="actionLabel" />.</param>
    /// <param name="timeout">
    ///     Optional timeout in seconds. When null, defaults to 6 seconds for non-action snackbars and 0 for action snackbars.
    /// </param>
    /// <param name="showClose">
    ///     Optional close-affordance flag. When null, defaults to false for non-action snackbars and true for action snackbars.
    /// </param>
    /// <param name="backgroundColor">Snackbar container color.</param>
    /// <param name="textColor">Supporting text color.</param>
    /// <param name="actionColor">Action label color.</param>
    Task ShowAsync(string message, string? actionLabel = null, Func<Task>? actionCallback = null, int? timeout = null, bool? showClose = null, TnTColor backgroundColor = TnTColor.InverseSurface, TnTColor textColor = TnTColor.InverseOnSurface, TnTColor actionColor = TnTColor.InversePrimary);
}
