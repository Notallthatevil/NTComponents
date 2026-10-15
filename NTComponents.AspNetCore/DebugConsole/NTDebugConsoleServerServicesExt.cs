using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NTComponents.AspNetCore.DebugConsole;
using NTComponents.DebugConsole;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Registers the NTComponents debug console for Blazor Server.
/// </summary>
public static class NTDebugConsoleServerServicesExt {

    /// <summary>
    ///     Adds the debug console for Blazor Server: each circuit's console captures only the <c>ILogger</c> output of that circuit's own work.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It calls <see cref="NTDebugConsoleServicesExt.AddNTDebugConsole" /> and adds a circuit handler that sets the circuit's session for every inbound activity (event handlers, renders,
    ///         JavaScript interop calls, and their awaited continuations) and evaluates <see cref="NTDebugConsoleOptions.Authorize" /> against the circuit's authentication state.
    ///     </para>
    ///     <para>
    ///         Not captured: work that does not flow from the circuit, such as background services, timers created outside the circuit, <c>Task.Run</c> with a suppressed execution context, and
    ///         other users' requests, and the prerendering request before the circuit connects. Work that one circuit's activity starts and that keeps running after it, such as a timer it creates,
    ///         stays attributed to that circuit.
    ///     </para>
    ///     <para>
    ///         The console shows logs to the user of the circuit, so set <see cref="NTDebugConsoleOptions.Authorize" /> to a rule that only admits people who may see them, such as
    ///         <c>state =&gt; state.User.IsInRole("Developer")</c>.
    ///     </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the console options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNTDebugConsoleForServer(this IServiceCollection services, Action<NTDebugConsoleOptions>? configure = null) {
        services.AddNTDebugConsole(configure);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, NTDebugConsoleCircuitHandler>());
        return services;
    }
}
