using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using NTComponents.Core;
using NTComponents.Dialog;

namespace NTComponents;

/// <summary>
///     Represents the possible results of a dialog operation.
/// </summary>
public enum DialogResult {

    /// <summary>
    ///     The dialog is pending.
    /// </summary>
    Pending,

    /// <summary>
    ///     The dialog has failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     The dialog has been closed.
    /// </summary>
    Closed,

    /// <summary>
    ///     The dialog has been cancelled. This is equivalent to <see cref="Closed" />.
    /// </summary>
    Cancelled = Closed,

    /// <summary>
    ///     The dialog has been confirmed.
    /// </summary>
    Confirmed,

    /// <summary>
    ///     The dialog has succeeded. This is equivalent to <see cref="Confirmed" />.
    /// </summary>
    Succeeded = Confirmed,

    /// <summary>
    ///     The dialog has been deleted.
    /// </summary>
    Deleted
}

/// <summary>
///     Represents a dialog component that can be used to display modal dialogs.
/// </summary>
public partial class TnTDialog {

    /// <summary>
    ///     Gets or sets the JavaScript runtime used to invoke JavaScript functions.
    /// </summary>
    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the dialog service used to manage dialogs.
    /// </summary>
    [Inject]
    private ITnTDialogService _service { get; set; } = default!;

    private readonly HashSet<ITnTDialog> _dialogs = [];

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <summary>
    ///     Disposes the dialog component and unsubscribes from events.
    /// </summary>
    public void Dispose() {
        _service.OnClose -= OnClose;
        _service.OnOpen -= OnOpen;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        ITnTDialog[] dialogs;
        lock (_lock) {
            dialogs = [.. _dialogs];
        }
        foreach (var dialog in dialogs) {
            await _jsRuntime.InvokeVoidAsync("NTComponents.openModalDialog", dialog.ElementId);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        _service.OnOpen += OnOpen;
        _service.OnClose += OnClose;
    }

    /// <summary>
    ///     Handles the close event for a dialog.
    /// </summary>
    /// <param name="dialog">The dialog to close.</param>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    private async Task OnClose(ITnTDialog dialog) {
        dialog.Options.Closing = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(150);
        lock (_lock) {
            _dialogs.Remove(dialog);
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Handles the open event for a dialog.
    /// </summary>
    /// <param name="dialog">The dialog to open.</param>
    /// <returns>A task that represents the asynchronous open operation.</returns>
    private async Task OnOpen(ITnTDialog dialog) {
        lock (_lock) {
            _dialogs.Add(dialog);
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Renders the content of a dialog.
    /// </summary>
    /// <param name="dialog">The dialog to render content for.</param>
    /// <returns>A render fragment that represents the content of the dialog.</returns>
    private RenderFragment RenderDialogContent(ITnTDialog dialog) {
        return new RenderFragment(builder => {
            if (dialog.Options.Title is not null || dialog.Options.ShowCloseButton) {
                builder.OpenElement(0, "div");
                builder.AddAttribute(10, "class", "tnt-dialog-header");
                {
                    if (dialog.Options.Title is not null) {
                        builder.OpenElement(20, "h2");
                        builder.AddContent(30, dialog.Options.Title);
                        builder.CloseElement();
                    }

                    if (dialog.Options.ShowCloseButton) {
                        builder.OpenComponent<TnTImageButton>(40);
                        builder.AddComponentParameter(50, nameof(TnTImageButton.Icon), MaterialIcon.Close);
                        builder.AddComponentParameter(51, nameof(TnTImageButton.Appearance), ButtonAppearance.Text);
                        builder.AddComponentParameter(52, nameof(TnTImageButton.TextColor), dialog.Options.TextColor);
                        builder.AddComponentParameter(53, nameof(TnTImageButton.ButtonSize), Size.XS);
                        builder.AddComponentParameter(54, nameof(TnTImageButton.AriaLabel), "Close dialog");
                        builder.AddComponentParameter(60, nameof(TnTImageButton.OnClickCallback), EventCallback.Factory.Create<MouseEventArgs>(this, dialog.CloseAsync));
                        builder.CloseComponent();
                    }
                }
                builder.CloseElement();

                builder.OpenComponent<TnTDivider>(70);
                builder.CloseComponent();
            }

            {
                builder.OpenComponent<CascadingValue<ITnTDialog>>(80);
                builder.AddAttribute(150, nameof(CascadingValue<ITnTDialog>.Value), dialog);
                builder.AddAttribute(160, nameof(CascadingValue<ITnTDialog>.IsFixed), true);
                builder.AddAttribute(170, nameof(CascadingValue<ITnTDialog>.ChildContent), new RenderFragment(cascadingBuilder => {
                    cascadingBuilder.OpenComponent(0, dialog.Type);
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                    // Disabling warning since value in these key value pairs is allowed to be null when the parameter on the component allows it. It is up to the caller when opening a dialog to set
                    // the parameters correctly.
                    cascadingBuilder.AddMultipleAttributes(10, dialog.Parameters);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                    cascadingBuilder.CloseComponent();
                }));

                builder.CloseComponent();
            }
        });
    }
}
