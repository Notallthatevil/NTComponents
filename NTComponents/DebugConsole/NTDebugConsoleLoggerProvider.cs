using Microsoft.Extensions.Logging;

namespace NTComponents.DebugConsole;

/// <summary>
///     Sends <see cref="ILogger" /> output to the debug console session of the work that wrote it.
/// </summary>
/// <remarks>
///     On Blazor Server, output reaches a circuit's console only when it flows from that circuit's inbound activity. Background services, timers created outside a circuit, work started with
///     a suppressed execution context, other users' requests, and the prerendering request before the circuit connects are not captured. On WebAssembly all output belongs to the one user.
/// </remarks>
/// <param name="_accessor">Finds the session of the current work.</param>
/// <param name="_options">The console options.</param>
internal sealed class NTDebugConsoleLoggerProvider(NTDebugConsoleSessionAccessor _accessor, NTDebugConsoleOptions _options) : ILoggerProvider {

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new NTDebugConsoleLogger(categoryName, IsExcluded(categoryName), _accessor, _options);

    /// <inheritdoc />
    public void Dispose() { }

    private bool IsExcluded(string categoryName) => _options.ExcludedCategoryPrefixes.Any(prefix => !string.IsNullOrEmpty(prefix) && categoryName.StartsWith(prefix, StringComparison.Ordinal));
}
