using System.Runtime.CompilerServices;

namespace NTComponents.DebugConsole;

/// <summary>
///     Finds the debug console session that a log written on the current thread belongs to.
/// </summary>
/// <remarks>
///     <para>
///         On Blazor Server the circuit handler sets the session for every inbound activity of a circuit. The session flows with the <see cref="ExecutionContext" />, so it reaches event
///         handlers, renders, JavaScript interop calls, and their awaited continuations, and never work started by another circuit.
///     </para>
///     <para>
///         Work running on a circuit's renderer dispatcher always belongs to that circuit, even when another circuit queued it, for example by raising a shared event that calls
///         <c>InvokeAsync</c> on this circuit's components. The dispatcher check comes first so that work is not attributed to the circuit that queued it.
///     </para>
///     <para>A single-session host, such as WebAssembly, falls back to its one session when no other session applies.</para>
/// </remarks>
/// <param name="singleSession">Whether the host runs one user's session, as WebAssembly does.</param>
internal sealed class NTDebugConsoleSessionAccessor(bool singleSession) {
    // Only Blazor's own renderer context is per circuit; any other context could be shared between circuits.
    private const string RendererSynchronizationContextTypeName = "Microsoft.AspNetCore.Components.Rendering.RendererSynchronizationContext";

    private readonly AsyncLocal<NTDebugConsoleService?> _current = new();
    private readonly ConditionalWeakTable<SynchronizationContext, NTDebugConsoleService> _dispatchers = [];
    private NTDebugConsoleService? _fallback;

    /// <summary>
    ///     Gets the session of the current work, or <see langword="null" /> when the work belongs to no session.
    /// </summary>
    public NTDebugConsoleService? Current {
        get {
            var context = SynchronizationContext.Current;
            if (context is not null && _dispatchers.TryGetValue(context, out var owner)) {
                return owner;
            }

            return _current.Value ?? (singleSession ? Volatile.Read(ref _fallback) : null);
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the host runs a single user's session.
    /// </summary>
    public bool SingleSession => singleSession;

    /// <summary>
    ///     Makes <paramref name="session" /> the session of the current execution context and the work that flows from it.
    /// </summary>
    /// <remarks>Call it from an <see langword="async" /> method so the change does not leak back into the caller's context.</remarks>
    public void Enter(NTDebugConsoleService? session) => _current.Value = session;

    /// <summary>
    ///     Makes <paramref name="session" /> the owner of all work that runs on <paramref name="context" />, when it is a Blazor renderer context.
    /// </summary>
    /// <returns><see langword="true" /> when the context was registered.</returns>
    public bool RegisterDispatcher(SynchronizationContext? context, NTDebugConsoleService session) {
        if (context?.GetType().FullName != RendererSynchronizationContextTypeName) {
            return false;
        }

        _dispatchers.AddOrUpdate(context, session);
        return true;
    }

    /// <summary>
    ///     Makes <paramref name="session" /> the session of a single-session host, unless one is already registered.
    /// </summary>
    public void RegisterFallback(NTDebugConsoleService session) {
        if (singleSession) {
            Interlocked.CompareExchange(ref _fallback, session, null);
        }
    }

    /// <summary>
    ///     Removes <paramref name="session" /> as the single-session fallback when it is the registered one.
    /// </summary>
    public void UnregisterFallback(NTDebugConsoleService session) => Interlocked.CompareExchange(ref _fallback, null, session);
}
