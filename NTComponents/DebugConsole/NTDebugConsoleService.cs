using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTComponents.Snackbar;
using NTComponents.Toast;
using System.Security.Claims;

namespace NTComponents.DebugConsole;

/// <summary>
///     The scoped debug console session: a thread-safe ring buffer of entries for one Blazor Server circuit or one WebAssembly app.
/// </summary>
internal sealed class NTDebugConsoleService : INTDebugConsole, IDisposable {
    internal const string AppCategory = "App";
    internal const string SnackbarCategory = "NTComponents.Snackbar";
    internal const string ToastCategory = "NTComponents.Toast";

    private readonly NTDebugConsoleSessionAccessor _accessor;
    private readonly NTDebugConsoleEntry?[] _buffer;
    private readonly Lock _lock = new();
    private readonly NTDebugConsoleOptions _options;
    private readonly INTSnackbarService? _snackbarService;
    private readonly INTToastService? _toastService;
    private AuthenticationStateProvider? _authenticationStateProvider;
    private bool _authorizationStarted;
    private int _authorizationVersion;
    private int _count;
    private volatile bool _disposed;
    private volatile bool _enabled;
    private int _start;

    public NTDebugConsoleService(NTDebugConsoleOptions options, NTDebugConsoleSessionAccessor accessor, IServiceProvider services) {
        _options = options;
        _accessor = accessor;
        _buffer = new NTDebugConsoleEntry?[Math.Max(1, options.MaxEntries)];

        if (options.MirrorToastsAndSnackbars) {
            _toastService = services.GetService<INTToastService>();
            _snackbarService = services.GetService<INTSnackbarService>();
            _toastService?.OnOpen += OnToastOpenAsync;
            _snackbarService?.OnOpen += OnSnackbarOpenAsync;
        }

        // A single-session host has no circuit handler, so the session registers and authorizes itself.
        if (accessor.SingleSession) {
            accessor.RegisterFallback(this);
            StartAuthorization(services.GetService<AuthenticationStateProvider>());
        }
    }

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public IReadOnlyList<NTDebugConsoleEntry> Entries {
        get {
            lock (_lock) {
                var entries = new NTDebugConsoleEntry[_count];
                for (var i = 0; i < _count; i++) {
                    entries[i] = _buffer[(_start + i) % _buffer.Length]!;
                }
                return entries;
            }
        }
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
    public void Clear() {
        lock (_lock) {
            ClearBuffer();
        }
        RaiseChanged();
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _toastService?.OnOpen -= OnToastOpenAsync;
        _snackbarService?.OnOpen -= OnSnackbarOpenAsync;
        _authenticationStateProvider?.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _accessor.UnregisterFallback(this);
        SetEnabled(false);
    }

    /// <inheritdoc />
    public void Write(LogLevel level, string message, string? category = null) {
        ArgumentNullException.ThrowIfNull(message);
        if (level != LogLevel.None) {
            Capture(new(DateTimeOffset.Now, level, string.IsNullOrWhiteSpace(category) ? AppCategory : category, message, null, default, NTDebugConsoleEntrySource.App));
        }
    }

    /// <summary>
    ///     Stores an entry when the session is enabled and <see cref="NTDebugConsoleOptions.Redact" /> keeps it.
    /// </summary>
    internal void Capture(NTDebugConsoleEntry entry) {
        if (!_enabled || Redact(entry) is not { } stored) {
            return;
        }

        lock (_lock) {
            // The session may have been disabled while the entry was being redacted.
            if (!_enabled) {
                return;
            }

            _buffer[(_start + _count) % _buffer.Length] = stored;
            if (_count < _buffer.Length) {
                _count++;
            }
            else {
                _start = (_start + 1) % _buffer.Length;
            }
        }
        RaiseChanged();
    }

    /// <summary>
    ///     Enables or disables the session. Disabling it clears the entries, so a later user of the same session cannot see them.
    /// </summary>
    internal void SetEnabled(bool enabled) {
        enabled &= !_disposed;
        lock (_lock) {
            if (_enabled == enabled) {
                return;
            }

            _enabled = enabled;
            if (!enabled) {
                ClearBuffer();
            }
        }
        RaiseChanged();
    }

    /// <summary>
    ///     Evaluates <see cref="NTDebugConsoleOptions.Authorize" /> now and whenever the authentication state changes. Only the first call has an effect.
    /// </summary>
    /// <param name="provider">The session's authentication state provider, or <see langword="null" /> to evaluate an unauthenticated state.</param>
    internal void StartAuthorization(AuthenticationStateProvider? provider) {
        if (_options.Authorize is null || _authorizationStarted || _disposed) {
            return;
        }

        _authorizationStarted = true;
        if (provider is null) {
            SetEnabled(IsAuthorized(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
            return;
        }

        _authenticationStateProvider = provider;
        provider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        Task<AuthenticationState> stateTask;
        try {
            stateTask = provider.GetAuthenticationStateAsync();
        }
        catch (Exception exception) {
            // Some providers throw until they have a user, such as a server provider before its circuit is set up.
            stateTask = Task.FromException<AuthenticationState>(exception);
        }
        OnAuthenticationStateChanged(stateTask);
    }

    private static string Join(string title, string? message) => string.IsNullOrWhiteSpace(message) ? title : $"{title}: {message}";

    private async Task ApplyAuthorizationAsync(Task<AuthenticationState> stateTask, int version) {
        bool allowed;
        try {
            allowed = IsAuthorized(await stateTask);
        }
        catch (Exception) {
            // A provider that cannot supply a state yet, such as before a circuit has its user, denies until it raises a change.
            allowed = false;
        }

        // Only the latest authentication state decides, even when an earlier one completes later.
        if (version == Volatile.Read(ref _authorizationVersion)) {
            SetEnabled(allowed);
        }
    }

    private void ClearBuffer() {
        Array.Clear(_buffer);
        _start = 0;
        _count = 0;
    }

    private bool IsAuthorized(AuthenticationState state) {
        try {
            return _options.Authorize?.Invoke(state) == true;
        }
        catch (Exception) {
            return false;
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> stateTask) => _ = ApplyAuthorizationAsync(stateTask, Interlocked.Increment(ref _authorizationVersion));

    private Task OnSnackbarOpenAsync(INTSnackbar snackbar) {
        Capture(new(DateTimeOffset.Now, LogLevel.Information, SnackbarCategory, snackbar.Message, null, default, NTDebugConsoleEntrySource.Snackbar));
        return Task.CompletedTask;
    }

    private Task OnToastOpenAsync(INTToast toast) {
        var level = toast.Variant switch {
            NTToastVariant.Error or NTToastVariant.Assert => LogLevel.Error,
            NTToastVariant.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };
        Capture(new(DateTimeOffset.Now, level, ToastCategory, Join(toast.Title, toast.Message), null, default, NTDebugConsoleEntrySource.Toast));
        return Task.CompletedTask;
    }

    private void RaiseChanged() {
        if (Changed is not { } changed) {
            return;
        }

        // A failing subscriber must not break the log call or the other subscribers.
        foreach (var handler in changed.GetInvocationList()) {
            try {
                ((Action)handler)();
            }
            catch (Exception) {
            }
        }
    }

    private NTDebugConsoleEntry? Redact(NTDebugConsoleEntry entry) {
        if (_options.Redact is not { } redact) {
            return entry;
        }

        try {
            return redact(entry);
        }
        catch (Exception) {
            return null;
        }
    }
}
