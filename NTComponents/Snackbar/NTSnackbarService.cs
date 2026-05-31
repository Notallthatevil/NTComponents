using Microsoft.JSInterop;
using NTComponents.Core;

namespace NTComponents.Snackbar;

/// <summary>
///     Service for coordinating snackbar presentation and action execution through the snackbar JavaScript module.
/// </summary>
internal sealed class NTSnackbarService(IJSRuntime _jsRuntime) : INTSnackbarService, IAsyncDisposable {
    private const string _dotNetActionMethod = nameof(InvokeActionFromJavaScript);
    private const string _dotNetCloseMethod = nameof(NotifyClosedFromJavaScript);
    private readonly Dictionary<string, NTSnackbarImplementation> _snackbarsById = [];
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private DotNetObjectReference<NTSnackbarService>? _dotNetReference;
    private IJSObjectReference? _module;

    internal INTSnackbar? ActiveSnackbar {
        get {
            lock (_snackbarsById) {
                return _snackbarsById.Values.FirstOrDefault();
            }
        }
    }

    internal IReadOnlyList<INTSnackbar> GetCurrentStack(int maxCount = 3) {
        lock (_snackbarsById) {
            return _snackbarsById.Values.Take(maxCount).ToArray();
        }
    }

    public event INTSnackbarService.OnCloseCallback? OnClose;

    public event INTSnackbarService.OnOpenCallback? OnOpen;

    public async Task CloseAsync(INTSnackbar snackbar) {
        if (snackbar is not NTSnackbarImplementation implementation || !TryGetTrackedSnackbar(implementation.Id, out var trackedSnackbar)) {
            return;
        }

        var module = await GetModuleAsync();
        var closed = await module.InvokeAsync<bool>("closeSnackbarFromBlazor", trackedSnackbar.Id);
        if (!closed || !RemoveTrackedSnackbar(trackedSnackbar.Id, out trackedSnackbar)) {
            return;
        }

        await (OnClose?.Invoke(trackedSnackbar) ?? Task.CompletedTask);
    }

    public async ValueTask DisposeAsync() {
        if (_module is not null) {
            await _module.DisposeAsync();
        }

        _dotNetReference?.Dispose();
        _moduleLock.Dispose();
    }

    public async Task InvokeActionAsync(INTSnackbar snackbar) {
        if (snackbar is not NTSnackbarImplementation implementation || implementation.ActionCallback is null) {
            return;
        }

        await implementation.ActionCallback();
        await CloseAsync(snackbar);
    }

    public async Task ShowAsync(
        string message,
        string? actionLabel = null,
        Func<Task>? actionCallback = null,
        int? timeout = null,
        bool? showClose = null,
        TnTColor backgroundColor = TnTColor.InverseSurface,
        TnTColor textColor = TnTColor.InverseOnSurface,
        TnTColor actionColor = TnTColor.InversePrimary
    ) {
        var hasAction = actionCallback is not null || !string.IsNullOrWhiteSpace(actionLabel);
        if (hasAction && (actionCallback is null || string.IsNullOrWhiteSpace(actionLabel))) {
            throw new ArgumentException("Snackbar actions require both an action label and an action callback.");
        }

        var snackbar = new NTSnackbarImplementation() {
            Id = TnTComponentIdentifier.NewId(),
            Message = message,
            ActionLabel = actionLabel,
            ActionCallback = actionCallback,
            Timeout = timeout ?? (hasAction ? 0 : 6),
            ShowClose = showClose ?? hasAction,
            BackgroundColor = backgroundColor,
            TextColor = textColor,
            ActionColor = actionColor
        };

        TrackSnackbar(snackbar);
        try {
            var module = await GetModuleAsync();
            await module.InvokeAsync<string>("queueSnackbar", JavaScriptSnackbarOptions.From(snackbar, DotNetReference));
        }
        catch {
            RemoveTrackedSnackbar(snackbar.Id, out _);
            throw;
        }

        await (OnOpen?.Invoke(snackbar) ?? Task.CompletedTask);
    }

    [JSInvokable]
    public async Task InvokeActionFromJavaScript(string id) {
        if (!TryGetTrackedSnackbar(id, out var snackbar) || snackbar.ActionCallback is null) {
            return;
        }

        await snackbar.ActionCallback();
    }

    [JSInvokable]
    public async Task NotifyClosedFromJavaScript(string id) {
        if (!RemoveTrackedSnackbar(id, out var snackbar)) {
            return;
        }

        await (OnClose?.Invoke(snackbar) ?? Task.CompletedTask);
    }

    private DotNetObjectReference<NTSnackbarService> DotNetReference => _dotNetReference ??= DotNetObjectReference.Create(this);

    private async ValueTask<IJSObjectReference> GetModuleAsync() {
        if (_module is not null) {
            return _module;
        }

        await _moduleLock.WaitAsync();
        try {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", NTSnackbar.JsModulePathValue);
            return _module;
        }
        finally {
            _moduleLock.Release();
        }
    }

    private bool RemoveTrackedSnackbar(string id, out NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            if (!_snackbarsById.Remove(id, out var trackedSnackbar)) {
                snackbar = default!;
                return false;
            }

            snackbar = trackedSnackbar;
            return true;
        }
    }

    private void TrackSnackbar(NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            _snackbarsById[snackbar.Id] = snackbar;
        }
    }

    private bool TryGetTrackedSnackbar(string id, out NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            if (!_snackbarsById.TryGetValue(id, out var trackedSnackbar)) {
                snackbar = default!;
                return false;
            }

            snackbar = trackedSnackbar;
            return true;
        }
    }

    /// <summary>
    ///     Internal snackbar implementation stored by the snackbar service.
    /// </summary>
    internal sealed class NTSnackbarImplementation : INTSnackbar {
        public string? ActionLabel { get; set; }
        internal Func<Task>? ActionCallback { get; set; }
        public TnTColor ActionColor { get; set; } = TnTColor.InversePrimary;
        public TnTColor BackgroundColor { get; set; } = TnTColor.InverseSurface;
        public bool Closing => false;
        public bool HasAction => ActionCallback is not null && !string.IsNullOrWhiteSpace(ActionLabel);
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = default!;
        public bool ShowClose { get; set; }
        public double Timeout { get; set; } = 6;
        public TnTColor TextColor { get; set; } = TnTColor.InverseOnSurface;
    }

    private sealed class JavaScriptSnackbarOptions {
        public required string ActionColor { get; init; }
        public required string? ActionLabel { get; init; }
        public required string BackgroundColor { get; init; }
        public required string DotNetActionMethod { get; init; }
        public required string DotNetCloseMethod { get; init; }
        public required DotNetObjectReference<NTSnackbarService> DotNetReference { get; init; }
        public required string Id { get; init; }
        public required string Message { get; init; }
        public required bool ShowClose { get; init; }
        public required string TextColor { get; init; }
        public required double Timeout { get; init; }

        public static JavaScriptSnackbarOptions From(NTSnackbarImplementation snackbar, DotNetObjectReference<NTSnackbarService> dotNetReference) {
            return new JavaScriptSnackbarOptions {
                ActionColor = snackbar.ActionColor.ToCssTnTColorVariable(),
                ActionLabel = snackbar.ActionLabel,
                BackgroundColor = snackbar.BackgroundColor.ToCssTnTColorVariable(),
                DotNetActionMethod = _dotNetActionMethod,
                DotNetCloseMethod = _dotNetCloseMethod,
                DotNetReference = dotNetReference,
                Id = snackbar.Id,
                Message = snackbar.Message,
                ShowClose = snackbar.ShowClose,
                TextColor = snackbar.TextColor.ToCssTnTColorVariable(),
                Timeout = snackbar.Timeout
            };
        }
    }
}
