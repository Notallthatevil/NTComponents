using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Represents a popover window that can be shown, hidden, moved, and closed.
/// </summary>
public interface INTPopoverHandle {

    /// <summary>
    ///     Gets inline child content when the popover was opened from a render fragment.
    /// </summary>
    RenderFragment? ChildContent { get; }

    /// <summary>
    ///     Gets the element identifier for the popover window.
    /// </summary>
    string ElementId { get; }

    /// <summary>
    ///     Gets a value indicating whether the popover is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    ///     Gets the left position of the popover in pixels.
    /// </summary>
    double Left { get; }

    /// <summary>
    ///     Gets the options used to configure the popover.
    /// </summary>
    NTPopoverOptions Options { get; }

    /// <summary>
    ///     Gets the component parameters used when rendering a typed popover.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Parameters { get; }

    /// <summary>
    ///     Gets the top position of the popover in pixels.
    /// </summary>
    double Top { get; }

    /// <summary>
    ///     Gets the component type used when rendering a typed popover.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? Type { get; }

    /// <summary>
    ///     Gets the stacking order of the popover window.
    /// </summary>
    int ZIndex { get; }

    /// <summary>
    ///     Brings the popover to the front of the stack.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BringToFrontAsync();

    /// <summary>
    ///     Closes the popover and removes it from the host.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CloseAsync();

    /// <summary>
    ///     Hides the popover while keeping it available for later restoration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HideAsync();

    /// <summary>
    ///     Shows the popover if it is currently hidden.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowAsync();
}
