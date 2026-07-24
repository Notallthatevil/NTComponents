using Microsoft.AspNetCore.Components;

namespace NTComponents;

/// <summary>
///     Represents a window managed by <see cref="INTWindowService" />.
/// </summary>
public interface INTWindow {

    /// <summary>
    ///     Gets where the window is docked when minimized.
    /// </summary>
    NTWindowDockPosition DockPosition { get; }

    /// <summary>
    ///     Gets the content rendered inside the window.
    /// </summary>
    RenderFragment Content { get; }

    /// <summary>
    ///     Gets the stable identifier assigned to the window.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Gets the current visible state of the window.
    /// </summary>
    NTWindowState State { get; }

    /// <summary>
    ///     Gets the visible window title.
    /// </summary>
    string Title { get; }
}
