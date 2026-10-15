using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace NTComponents.DebugConsole;

/// <summary>
///     Configures what the debug console captures and who may use it.
/// </summary>
/// <remarks>
///     The console is disabled until <see cref="Authorize" /> is set. On Blazor Server it shows each circuit's own logs to that circuit's user, so set <see cref="Authorize" /> to a rule that only
///     admits people who may see those logs, such as developers or support staff. A WebAssembly app runs in one user's browser, so <see cref="AllowAll" /> is safe there.
/// </remarks>
public sealed class NTDebugConsoleOptions {

    /// <summary>
    ///     Gets or sets the rule that decides whether the current user may use the console. When <see langword="null" />, the default, the console is disabled: nothing is captured and
    ///     <see cref="NTDebugConsole" /> renders nothing.
    /// </summary>
    /// <remarks>
    ///     It is evaluated when the Blazor Server circuit opens or the WebAssembly app starts, and again whenever the authentication state changes. Without an
    ///     <see cref="AuthenticationStateProvider" />, it receives an unauthenticated state. Disabling the console clears its entries. A rule that throws is treated as denied.
    /// </remarks>
    public Func<AuthenticationState, bool>? Authorize { get; set; }

    /// <summary>
    ///     Gets the logger category prefixes whose output is never captured. Matching is ordinal and case-sensitive.
    /// </summary>
    /// <remarks>
    ///     The defaults exclude the console's own categories, so updating the console cannot capture more entries, and the per-render, JavaScript interop, and SignalR transport categories of
    ///     the framework, which log for every message when <see cref="MinimumLevel" /> is lowered to <see cref="LogLevel.Debug" /> or <see cref="LogLevel.Trace" />.
    /// </remarks>
    public ICollection<string> ExcludedCategoryPrefixes { get; } = [
        "NTComponents.DebugConsole",
        "NTComponents.NTDebugConsole",
        "Microsoft.AspNetCore.Components.RenderTree",
        "Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer",
        "Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime",
        "Microsoft.AspNetCore.SignalR",
        "Microsoft.AspNetCore.Http.Connections"
    ];

    /// <summary>
    ///     Gets or sets a value indicating whether a logged exception keeps its full <see cref="Exception.ToString" /> text, including inner exceptions and stack traces. When
    ///     <see langword="false" />, only the exception type and message are kept. Defaults to <see langword="true" />.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = true;

    /// <summary>
    ///     Gets or sets the most entries the console keeps. When it is full, each new entry drops the oldest. Defaults to 1000 and must be at least 1.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;

    /// <summary>
    ///     Gets or sets the lowest <see cref="ILogger" /> level that is captured. Defaults to <see cref="LogLevel.Information" />. It does not apply to <c>INTDebugConsole.Write</c> or to
    ///     mirrored toasts and snackbars.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     Gets or sets a value indicating whether toasts and snackbars shown through <see cref="INTToastService" /> and <see cref="INTSnackbarService" /> are added to the console, so it records
    ///     the feedback the user saw. Defaults to <see langword="true" />.
    /// </summary>
    public bool MirrorToastsAndSnackbars { get; set; } = true;

    /// <summary>
    ///     Gets or sets a function that rewrites each entry before it is stored, for example to mask tokens or personal data. Return <see langword="null" /> to drop the entry. A function that
    ///     throws drops the entry.
    /// </summary>
    public Func<NTDebugConsoleEntry, NTDebugConsoleEntry?>? Redact { get; set; }

    /// <summary>
    ///     Lets every user use the console. Use it only where all captured logs belong to the person viewing them, such as a WebAssembly app or a development environment.
    /// </summary>
    /// <returns>These options, for chaining.</returns>
    public NTDebugConsoleOptions AllowAll() {
        Authorize = static _ => true;
        return this;
    }
}
