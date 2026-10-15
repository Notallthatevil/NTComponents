using Microsoft.Extensions.Logging;

namespace NTComponents.DebugConsole;

/// <summary>
///     Writes one category's log entries to the debug console session of the current work.
/// </summary>
/// <param name="_category">The logger category.</param>
/// <param name="_excluded">Whether the category matches <see cref="NTDebugConsoleOptions.ExcludedCategoryPrefixes" />.</param>
/// <param name="_accessor">Finds the session of the current work.</param>
/// <param name="_options">The console options.</param>
internal sealed class NTDebugConsoleLogger(string _category, bool _excluded, NTDebugConsoleSessionAccessor _accessor, NTDebugConsoleOptions _options) : ILogger {

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => GetSession(logLevel) is not null;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        // Everything is checked before the message is formatted, so output nobody can see costs almost nothing.
        if (GetSession(logLevel) is not { } session) {
            return;
        }

        var exceptionText = exception is null ? null : _options.IncludeExceptionDetails ? exception.ToString() : $"{exception.GetType().FullName}: {exception.Message}";
        session.Capture(new(DateTimeOffset.Now, logLevel, _category, formatter(state, exception), exceptionText, eventId, NTDebugConsoleEntrySource.Log));
    }

    private NTDebugConsoleService? GetSession(LogLevel logLevel) =>
        _excluded || _options.Authorize is null || logLevel == LogLevel.None || logLevel < _options.MinimumLevel
            ? null
            : _accessor.Current is { IsEnabled: true } session ? session : null;
}
