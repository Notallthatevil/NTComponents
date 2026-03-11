using Microsoft.JSInterop;
using NTComponents.Core;
using System.Collections.Concurrent;

namespace NTComponents.Toast;

/// <summary>
///     Service for managing toast notifications.
/// </summary>
internal class TnTToastService : ITnTToastService {
    private readonly ConcurrentDictionary<ITnTToast, byte> _activeToasts = new();

    internal IReadOnlyCollection<ITnTToast> ActiveToasts => _activeToasts.Keys.ToArray();

    public event ITnTToastService.OnCloseCallback? OnClose;

    public event ITnTToastService.OnOpenCallback? OnOpen;

    public async Task CloseAsync(ITnTToast toast) {
        _activeToasts.TryRemove(toast, out _);
        await (OnClose?.Invoke(toast) ?? Task.CompletedTask);
    }

    public async Task ShowAsync(string title, string? message = null, int timeout = 10, bool showClose = true, TnTColor backgroundColor = TnTColor.SurfaceVariant, TnTColor textColor = TnTColor.OnSurfaceVariant) {
        var toast = new TnTToastImplementation() {
            Timeout = timeout,
            ShowClose = showClose,
            Title = title,
            Message = message,
            BackgroundColor = backgroundColor,
            TextColor = textColor
        };

        _activeToasts.TryAdd(toast, 0);
        await (OnOpen?.Invoke(toast) ?? Task.CompletedTask);
    }

    public async Task ShowErrorAsync(string title, string? message = null, int timeout = 10, bool showClose = true) =>
        await ShowAsync(title, message, timeout, showClose, TnTColor.ErrorContainer, TnTColor.Error);

    public async Task ShowErrorAsync(string title, Exception? message, int timeout = 10, bool showClose = true) =>
        await ShowErrorAsync(title, message?.Message, timeout, showClose);

    public async Task ShowInfoAsync(string title, string? message = null, int timeout = 10, bool showClose = true) =>
        await ShowAsync(title, message, timeout, showClose, TnTColor.InfoContainer, TnTColor.Info);

    public async Task ShowSuccessAsync(string title, string? message = null, int timeout = 10, bool showClose = true) =>
        await ShowAsync(title, message, timeout, showClose, TnTColor.SuccessContainer, TnTColor.Success);

    public async Task ShowWarningAsync(string title, string? message = null, int timeout = 10, bool showClose = true) =>
        await ShowAsync(title, message, timeout, showClose, TnTColor.WarningContainer, TnTColor.Warning);

    /// <summary>
    ///     Implementation of the ITnTToast interface.
    /// </summary>
    internal class TnTToastImplementation : ITnTToast {
        public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceVariant;
        public bool Closing { get; internal set; }
        public string? Message { get; set; }
        public bool ShowClose { get; set; } = true;
        public TextAlign? TextAlignment { get; }
        public TnTColor TextColor { get; set; } = TnTColor.OnSurfaceVariant;
        public double Timeout { get; set; } = 10;
        public string Title { get; set; } = default!;
    }
}
