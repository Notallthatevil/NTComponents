using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using NTComponents.Core;
using NTComponents.Toast;
using static NTComponents.Toast.TnTToastService;

namespace NTComponents;

/// <summary>
///     Represents a toast notification component that can display multiple toasts.
/// </summary>
public partial class TnTToast {
    private const int _closeDelay = 250;
    private const int _ssrCloseAnimationDelay = 500;

    private const string SsrCloseOnClick =
        "const toast = event.currentTarget.closest('.tnt-toast'); if (toast) { toast.classList.add('tnt-closing'); setTimeout(() => toast.remove(), 500); }";

    /// <summary>
    ///     Gets or sets the toast service used to manage toasts.
    /// </summary>
    [Inject]
    private ITnTToastService _service { get; set; } = default!;

    private readonly ConcurrentDictionary<ITnTToast, ToastMetadata> _toasts = new();

    private readonly CancellationTokenSource _tokenSource = new();

    /// <summary>
    ///     Disposes the resources used by the component.
    /// </summary>
    public void Dispose() {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        _service.OnClose -= OnClose;
        _service.OnOpen -= OnOpen;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        _service.OnOpen += OnOpen;
        _service.OnClose += OnClose;
    }

    /// <summary>
    ///     Handles the close event for a toast.
    /// </summary>
    /// <param name="toast">The toast to close.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OnClose(ITnTToast toast) {
        if (toast is TnTToastImplementation impl) {
            impl.Closing = true;
        }
        await InvokeAsync(StateHasChanged);
        await Task.Delay(_closeDelay);

        _toasts.Remove(toast, out _);

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Handles the open event for a toast.
    /// </summary>
    /// <param name="toast">The toast to open.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task OnOpen(ITnTToast toast) {
        _toasts.TryAdd(toast, new ToastMetadata() { CreatedTime = DateTimeOffset.Now, Task = null, Id = TnTComponentIdentifier.NewId() });
        await InvokeAsync(StateHasChanged);

    }

    private static string? GetSsrAutoDismissScript(ITnTToast toast, ToastMetadata metadata) {
        if (toast.Timeout <= 0) {
            return null;
        }

        var timeoutMilliseconds = (int)TimeSpan.FromSeconds(toast.Timeout).TotalMilliseconds;

        return $$"""
                 setTimeout(() => {
                     const toast = document.querySelector('#{{metadata.Id}}');
                     if (!toast) {
                         return;
                     }

                     toast.classList.add('tnt-closing');
                     setTimeout(() => toast.remove(), {{_ssrCloseAnimationDelay}});
                 }, {{timeoutMilliseconds}});
                 """;
    }

    private struct ToastMetadata {
        public required DateTimeOffset CreatedTime { get; set; }
        public required Task? Task { get; set; }
        public required string Id { get; set; }
    }
}
