using NTComponents.Core;

namespace NTComponents.Snackbar;

/// <summary>
///     Service for coordinating snackbar presentation and action execution.
/// </summary>
internal class NTSnackbarService : INTSnackbarService {
    private readonly Queue<INTSnackbar> _pendingSnackbars = new();
    private readonly object _sync = new();

    internal event Func<Task>? QueueChanged;

    internal INTSnackbar? ActiveSnackbar {
        get {
            lock (_sync) {
                return _activeSnackbar;
            }
        }
    }

    internal IReadOnlyList<INTSnackbar> GetCurrentStack(int maxCount = 3) {
        lock (_sync) {
            var stack = new List<INTSnackbar>(maxCount);
            if (_activeSnackbar is not null) {
                stack.Add(_activeSnackbar);
            }

            foreach (var snackbar in _pendingSnackbars) {
                if (stack.Count >= maxCount) {
                    break;
                }

                stack.Add(snackbar);
            }

            return stack;
        }
    }

    private INTSnackbar? _activeSnackbar;

    public event INTSnackbarService.OnCloseCallback? OnClose;

    public event INTSnackbarService.OnOpenCallback? OnOpen;

    public async Task CloseAsync(INTSnackbar snackbar) {
        INTSnackbar? nextSnackbar = null;
        var removedQueuedSnackbar = false;
        var snackbarWasClosed = false;

        lock (_sync) {
            if (ReferenceEquals(_activeSnackbar, snackbar)) {
                snackbarWasClosed = true;
                _activeSnackbar = null;

                if (_pendingSnackbars.Count > 0) {
                    nextSnackbar = _pendingSnackbars.Dequeue();
                    _activeSnackbar = nextSnackbar;
                }
            }
            else {
                snackbarWasClosed = RemovePendingSnackbar(snackbar);
                removedQueuedSnackbar = snackbarWasClosed;
            }
        }

        if (!snackbarWasClosed) {
            return;
        }

        await (OnClose?.Invoke(snackbar) ?? Task.CompletedTask);

        if (removedQueuedSnackbar) {
            await (QueueChanged?.Invoke() ?? Task.CompletedTask);
        }

        if (nextSnackbar is not null) {
            await (OnOpen?.Invoke(nextSnackbar) ?? Task.CompletedTask);
        }
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
            Message = message,
            ActionLabel = actionLabel,
            ActionCallback = actionCallback,
            Timeout = timeout ?? (hasAction ? 0 : 6),
            ShowClose = showClose ?? hasAction,
            BackgroundColor = backgroundColor,
            TextColor = textColor,
            ActionColor = actionColor
        };

        var shouldOpen = false;
        lock (_sync) {
            if (_activeSnackbar is null) {
                _activeSnackbar = snackbar;
                shouldOpen = true;
            }
            else {
                _pendingSnackbars.Enqueue(snackbar);
            }
        }

        if (shouldOpen) {
            await (OnOpen?.Invoke(snackbar) ?? Task.CompletedTask);
        }
        else {
            await (QueueChanged?.Invoke() ?? Task.CompletedTask);
        }
    }

    private bool RemovePendingSnackbar(INTSnackbar snackbar) {
        if (_pendingSnackbars.Count == 0) {
            return false;
        }

        var snackbarWasRemoved = false;
        var count = _pendingSnackbars.Count;
        for (var i = 0; i < count; i++) {
            var pendingSnackbar = _pendingSnackbars.Dequeue();
            if (!snackbarWasRemoved && ReferenceEquals(pendingSnackbar, snackbar)) {
                snackbarWasRemoved = true;
                continue;
            }

            _pendingSnackbars.Enqueue(pendingSnackbar);
        }

        return snackbarWasRemoved;
    }

    /// <summary>
    ///     Internal snackbar implementation stored by the snackbar service.
    /// </summary>
    internal class NTSnackbarImplementation : INTSnackbar {
        public string? ActionLabel { get; set; }
        internal Func<Task>? ActionCallback { get; set; }
        public TnTColor ActionColor { get; set; } = TnTColor.InversePrimary;
        public TnTColor BackgroundColor { get; set; } = TnTColor.InverseSurface;
        public bool Closing { get; internal set; }
        public bool HasAction => ActionCallback is not null && !string.IsNullOrWhiteSpace(ActionLabel);
        public string Message { get; set; } = default!;
        public bool ShowClose { get; set; }
        public double Timeout { get; set; } = 6;
        public TnTColor TextColor { get; set; } = TnTColor.InverseOnSurface;
    }
}
