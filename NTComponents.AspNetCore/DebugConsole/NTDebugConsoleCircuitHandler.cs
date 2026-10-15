using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.DebugConsole;

namespace NTComponents.AspNetCore.DebugConsole;

/// <summary>
///     Ties a Blazor Server circuit's work to that circuit's debug console session and decides whether the circuit's user may use it.
/// </summary>
/// <remarks>
///     Every inbound activity of the circuit (event handlers, renders, JavaScript interop calls, navigation, and their awaited continuations) runs with the circuit's session, so
///     <c>ILogger</c> output from that work reaches only this circuit's console. Work that does not flow from the circuit, such as background services, timers created outside the circuit,
///     <c>Task.Run</c> with a suppressed execution context, other users' requests, and the prerendering request before the circuit connects, is not captured.
/// </remarks>
/// <param name="_session">The circuit's session.</param>
/// <param name="_accessor">Finds the session of the current work.</param>
/// <param name="_services">The circuit's service provider.</param>
internal sealed class NTDebugConsoleCircuitHandler(NTDebugConsoleService _session, NTDebugConsoleSessionAccessor _accessor, IServiceProvider _services) : CircuitHandler {

    /// <summary>
    ///     Runs first, so the work of every later circuit handler is captured too.
    /// </summary>
    public override int Order => int.MinValue;

    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) => async context => {
        // Inside an async method the change flows into the activity and its continuations but not back to the caller.
        _accessor.Enter(_session);
        await next(context);
    };

    /// <inheritdoc />
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
        // Circuit handlers run on the circuit's renderer dispatcher, so work later queued there belongs to this circuit.
        _accessor.RegisterDispatcher(SynchronizationContext.Current, _session);
        _session.StartAuthorization(_services.GetService<AuthenticationStateProvider>());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) {
        _session.SetEnabled(false);
        return Task.CompletedTask;
    }
}
