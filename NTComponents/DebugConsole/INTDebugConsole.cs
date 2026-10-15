using Microsoft.Extensions.Logging;
using NTComponents.DebugConsole;

namespace NTComponents;

/// <summary>
///     The debug console session of the current user: the captured <see cref="ILogger" /> output, app messages, and mirrored toasts and snackbars that <see cref="NTDebugConsole" /> shows.
/// </summary>
/// <remarks>
///     <para>
///         Register it with <c>AddNTDebugConsole</c> for WebAssembly, or <c>AddNTDebugConsoleForServer</c> from the NTComponents.AspNetCore package for Blazor Server. It
///         is a scoped service, so there is one session per Blazor Server circuit and one for a WebAssembly app.
///     </para>
///     <para>
///         The session is disabled until <see cref="NTDebugConsoleOptions.Authorize" /> allows the current user. While it is disabled nothing is captured and <see cref="Write" /> does nothing. The
///         buffer keeps the newest <see cref="NTDebugConsoleOptions.MaxEntries" /> entries and drops the oldest. Members are thread-safe, because logs arrive from any thread.
///     </para>
/// </remarks>
public interface INTDebugConsole {

    /// <summary>
    ///     Occurs when an entry is added, the entries are cleared, or <see cref="IsEnabled" /> changes. It can be raised on any thread.
    /// </summary>
    event Action? Changed;

    /// <summary>
    ///     Gets a snapshot of the captured entries, oldest first.
    /// </summary>
    IReadOnlyList<NTDebugConsoleEntry> Entries { get; }

    /// <summary>
    ///     Gets a value indicating whether the current user is allowed to use the console. Nothing is captured while it is <see langword="false" />.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Removes all captured entries.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Adds an app or user message to the console, such as a step the user took or a value the app received.
    /// </summary>
    /// <remarks>
    ///     <see cref="NTDebugConsoleOptions.MinimumLevel" /> and <see cref="NTDebugConsoleOptions.ExcludedCategoryPrefixes" /> apply only to captured <see cref="ILogger" /> output, so every written
    ///     message is kept unless <see cref="NTDebugConsoleOptions.Redact" /> drops it. <see cref="LogLevel.None" /> messages are ignored.
    /// </remarks>
    /// <param name="level">The level of the message.</param>
    /// <param name="message">The message text.</param>
    /// <param name="category">An optional category. Defaults to <c>App</c>.</param>
    void Write(LogLevel level, string message, string? category = null);
}
