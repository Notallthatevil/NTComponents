using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Dialog;

namespace NTComponents;

/// <summary>
/// Provides methods and events for managing dialogs.
/// </summary>
[Obsolete("ITnTDialogService is obsolete. Use owned NTDialog components with @ref, native command/commandfor attributes, or JavaScript by id instead.")]
public interface ITnTDialogService {

    /// <summary>
    /// Occurs when a dialog is closed.
    /// </summary>
    event OnCloseCallback? OnClose;

    /// <summary>
    /// Occurs when a dialog is opened.
    /// </summary>
    event OnOpenCallback? OnOpen;

    /// <summary>
    /// Represents the method that will handle the OnClose event.
    /// </summary>
    /// <param name="dialog">The dialog that was closed.</param>
    delegate Task OnCloseCallback(ITnTDialog dialog);

    /// <summary>
    /// Represents the method that will handle the OnOpen event.
    /// </summary>
    /// <param name="dialog">The dialog that was opened.</param>
    delegate Task OnOpenCallback(ITnTDialog dialog);

    /// <summary>
    /// Asynchronously closes the specified dialog.
    /// </summary>
    /// <param name="dialog">The dialog to close.</param>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    Task CloseAsync(ITnTDialog dialog);

    /// <summary>
    /// Asynchronously opens a dialog with the specified component.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the dialog.</typeparam>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <param name="parameters">The parameters to pass to the component.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the opened dialog.</returns>
    Task<ITnTDialog> OpenAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(TnTDialogOptions? options = null, IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : IComponent;

    /// <summary>
    /// Asynchronously opens a dialog with the specified render fragment.
    /// </summary>
    /// <param name="renderFragment">The render fragment to display in the dialog.</param>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the opened dialog.</returns>
    Task<ITnTDialog> OpenAsync(RenderFragment renderFragment, TnTDialogOptions? options = null);

    /// <summary>
    /// Asynchronously opens a dialog with the specified component and returns the result.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the dialog.</typeparam>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <param name="parameters">The parameters to pass to the component.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the dialog result.</returns>
    Task<DialogResult> OpenForResultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(TnTDialogOptions? options = null, IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : IComponent;

    /// <summary>
    /// Asynchronously opens a dialog with the specified render fragment and returns the result.
    /// </summary>
    /// <param name="renderFragment">The render fragment to display in the dialog.</param>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the dialog result.</returns>
    Task<DialogResult> OpenForResultAsync(RenderFragment renderFragment, TnTDialogOptions? options = null);
}

/// <summary>
/// Provides extension methods for <see cref="ITnTDialogService"/>.
/// </summary>
public static class ITnTDialogServiceExt {
    /// <summary>
    /// Opens a confirmation dialog asynchronously.
    /// </summary>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="confirmationCallback">The callback to invoke when the confirm button is clicked.</param>
    /// <param name="confirmButtonText">The text for the confirm button.</param>
    /// <param name="body">The body text of the dialog.</param>
    /// <param name="cancelButtonText">The text for the cancel button.</param>
    /// <param name="cancelButtonTextColor">The color of the cancel button text.</param>
    /// <param name="cancellationCallback">The callback to invoke when the cancel button is clicked.</param>
    /// <param name="showCancelButton">Whether to show the cancel button.</param>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the opened dialog.</returns>
    public static Task<ITnTDialog> OpenConfirmationDialogAsync(this ITnTDialogService dialogService,
        EventCallback confirmationCallback,
        string confirmButtonText = "Confirm",
        string body = "Are you sure?",
        string cancelButtonText = "Cancel",
        TnTColor cancelButtonTextColor = TnTColor.OnSurface,
        EventCallback cancellationCallback = default,
        bool showCancelButton = true,
        TnTDialogOptions? options = null) =>
        dialogService.OpenAsync<BasicConfirmationDialog>(options, new Dictionary<string, object?> {
            { nameof(BasicConfirmationDialog.Body), body },
            { nameof(BasicConfirmationDialog.CancelButtonText), cancelButtonText },
            { nameof(BasicConfirmationDialog.CancelButtonTextColor), cancelButtonTextColor },
            { nameof(BasicConfirmationDialog.CancelClickedCallback), cancellationCallback },
            { nameof(BasicConfirmationDialog.ConfirmButtonText), confirmButtonText },
            { nameof(BasicConfirmationDialog.ConfirmClickedCallback), confirmationCallback },
            { nameof(BasicConfirmationDialog.ShowCancelButton), showCancelButton }
        });

    /// <summary>
    /// Opens a confirmation dialog asynchronously and returns the result.
    /// </summary>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="confirmationCallback">The callback to invoke when the confirm button is clicked.</param>
    /// <param name="confirmButtonText">The text for the confirm button.</param>
    /// <param name="body">The body text of the dialog.</param>
    /// <param name="cancelButtonText">The text for the cancel button.</param>
    /// <param name="cancelButtonTextColor">The color of the cancel button text.</param>
    /// <param name="cancellationCallback">The callback to invoke when the cancel button is clicked.</param>
    /// <param name="showCancelButton">Whether to show the cancel button.</param>
    /// <param name="options">The options for configuring the dialog.</param>
    /// <returns>A task that represents the asynchronous open operation. The task result contains the dialog result.</returns>
    public static Task<DialogResult> OpenConfirmationDialogForResultAsync(this ITnTDialogService dialogService,
        EventCallback confirmationCallback,
        string confirmButtonText = "Confirm",
        string body = "Are you sure?",
        string cancelButtonText = "Cancel",
        TnTColor cancelButtonTextColor = TnTColor.OnSurface,
        EventCallback cancellationCallback = default,
        bool showCancelButton = true,
        TnTDialogOptions? options = null) =>
        dialogService.OpenForResultAsync<BasicConfirmationDialog>(options, new Dictionary<string, object?> {
            { nameof(BasicConfirmationDialog.Body), body },
            { nameof(BasicConfirmationDialog.CancelButtonText), cancelButtonText },
            { nameof(BasicConfirmationDialog.CancelButtonTextColor), cancelButtonTextColor },
            { nameof(BasicConfirmationDialog.CancelClickedCallback), cancellationCallback },
            { nameof(BasicConfirmationDialog.ConfirmButtonText), confirmButtonText },
            { nameof(BasicConfirmationDialog.ConfirmClickedCallback), confirmationCallback },
            { nameof(BasicConfirmationDialog.ShowCancelButton), showCancelButton }
        });
}
