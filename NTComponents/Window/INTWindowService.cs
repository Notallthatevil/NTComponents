using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Coordinates windows that should remain available while the user navigates within an interactive application.
/// </summary>
/// <remarks>
///     Register the service with <c>AddNTServices</c> and place one <see cref="NTWindowHost" /> in a persistent layout.
///     The scoped service preserves its windows for the lifetime of the Blazor circuit or WebAssembly application. Static
///     SSR creates a new scope for each independent request, so cross-request persistence requires application-owned state.
/// </remarks>
public interface INTWindowService {

    /// <summary>
    ///     Raised after the managed window collection or a managed window state changes.
    /// </summary>
    event Action? Changed;

    /// <summary>
    ///     Gets a snapshot of the currently managed windows in opening order.
    /// </summary>
    IReadOnlyList<INTWindow> Windows { get; }

    /// <summary>
    ///     Closes a managed window.
    /// </summary>
    /// <param name="window">The window to close.</param>
    void Close(INTWindow window);

    /// <summary>
    ///     Opens a window containing the supplied content.
    /// </summary>
    /// <param name="title">The visible and accessible window title.</param>
    /// <param name="content">The content to render inside the window.</param>
    /// <param name="initialState">The initial visible state.</param>
    /// <param name="dockPosition">Where the window is docked when minimized.</param>
    /// <returns>A handle that can be passed to <see cref="Close" /> or <see cref="SetState" />.</returns>
    INTWindow Open(string title, RenderFragment content, NTWindowState initialState = NTWindowState.Normal, NTWindowDockPosition dockPosition = NTWindowDockPosition.BottomRight);

    /// <summary>
    ///     Changes the state of a managed window.
    /// </summary>
    /// <param name="window">The window to update.</param>
    /// <param name="state">The new visible state.</param>
    void SetState(INTWindow window, NTWindowState state);
}
