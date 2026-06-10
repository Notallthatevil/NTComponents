using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents;

/// <summary>
///     Interface for the TnT Toast Service, providing methods to show and close toast notifications.
/// </summary>
[Obsolete("ITnTToastService is obsolete. Use NTToast with INTToastService instead.")]
public interface ITnTToastService {

    /// <summary>
    ///     Event triggered when a toast is closed.
    /// </summary>
    public event OnCloseCallback? OnClose;

    /// <summary>
    ///     Delegate for the OnClose event.
    /// </summary>
    /// <param name="toast">The toast that was closed.</param>
    public delegate Task OnCloseCallback(ITnTToast toast);

    /// <summary>
    ///     Event triggered when a toast is opened.
    /// </summary>
    public event OnOpenCallback? OnOpen;

    /// <summary>
    ///     Delegate for the OnOpen event.
    /// </summary>
    /// <param name="toast">The toast that was opened.</param>
    public delegate Task OnOpenCallback(ITnTToast toast);

    /// <summary>
    ///     Closes the specified toast asynchronously.
    /// </summary>
    /// <param name="toast">The toast to close.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CloseAsync(ITnTToast toast);

    /// <summary>
    ///     Shows a toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">          The title of the toast.</param>
    /// <param name="message">        The message of the toast.</param>
    /// <param name="timeout">        The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">      Indicates whether the close button should be shown.</param>
    /// <param name="backgroundColor">The background color of the toast.</param>
    /// <param name="textColor">      The text color of the toast.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowAsync(string title, string? message = null, int timeout = 10, bool showClose = true, TnTColor backgroundColor = TnTColor.SurfaceVariant, TnTColor textColor = TnTColor.OnSurfaceVariant);

    /// <summary>
    ///     Shows an error toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">       The title of the toast.</param>
    /// <param name="message">     The message of the toast.</param>
    /// <param name="timeout">     The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">   Indicates whether the close button should be shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, string? message = null, int timeout = 10, bool showClose = true);

    /// <summary>
    ///     Shows an error toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">       The title of the toast.</param>
    /// <param name="message">     The exception message to be displayed in the toast.</param>
    /// <param name="timeout">     The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">   Indicates whether the close button should be shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, Exception? message, int timeout = 10, bool showClose = true);

    /// <summary>
    ///     Shows an informational toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">       The title of the toast.</param>
    /// <param name="message">     The message of the toast.</param>
    /// <param name="timeout">     The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">   Indicates whether the close button should be shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowInfoAsync(string title, string? message = null, int timeout = 10, bool showClose = true);

    /// <summary>
    ///     Shows a success toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">       The title of the toast.</param>
    /// <param name="message">     The message of the toast.</param>
    /// <param name="timeout">     The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">   Indicates whether the close button should be shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowSuccessAsync(string title, string? message = null, int timeout = 10, bool showClose = true);

    /// <summary>
    ///     Shows a warning toast with the specified parameters asynchronously.
    /// </summary>
    /// <param name="title">       The title of the toast.</param>
    /// <param name="message">     The message of the toast.</param>
    /// <param name="timeout">     The timeout duration in seconds before the toast automatically closes.</param>
    /// <param name="showClose">   Indicates whether the close button should be shown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowWarningAsync(string title, string? message = null, int timeout = 10, bool showClose = true);
}
