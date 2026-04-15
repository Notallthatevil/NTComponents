using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Provides methods for opening and managing floating popover windows.
/// </summary>
public interface INTPopoverService {

    /// <summary>
    ///     Occurs when the popover collection or popover state changes.
    /// </summary>
    event PopoversChangedCallback? OnChanged;

    /// <summary>
    ///     Represents the method that handles popover collection changes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    delegate Task PopoversChangedCallback();

    /// <summary>
    ///     Brings the specified popover to the front.
    /// </summary>
    /// <param name="popover">The popover to activate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BringToFrontAsync(INTPopoverHandle popover);

    /// <summary>
    ///     Closes the specified popover.
    /// </summary>
    /// <param name="popover">The popover to close.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CloseAsync(INTPopoverHandle popover);

    /// <summary>
    ///     Gets all currently tracked popovers.
    /// </summary>
    /// <returns>A snapshot of the tracked popovers.</returns>
    IReadOnlyList<INTPopoverHandle> GetPopovers();

    /// <summary>
    ///     Hides the specified popover.
    /// </summary>
    /// <param name="popover">The popover to hide.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HideAsync(INTPopoverHandle popover);

    /// <summary>
    ///     Opens a popover for a render fragment.
    /// </summary>
    /// <param name="renderFragment">The content to render inside the popover.</param>
    /// <param name="options">The popover options. When <see cref="NTPopoverOptions.InstanceKey" /> is provided, an existing matching popover is reused.</param>
    /// <returns>The created or reused popover handle.</returns>
    Task<INTPopoverHandle> OpenAsync(RenderFragment renderFragment, NTPopoverOptions? options = null);

    /// <summary>
    ///     Opens a popover for a component type.
    /// </summary>
    /// <typeparam name="TComponent">The component type to render inside the popover.</typeparam>
    /// <param name="options">The popover options. When <see cref="NTPopoverOptions.InstanceKey" /> is provided, an existing matching popover is reused.</param>
    /// <param name="parameters">Parameters passed to the rendered component.</param>
    /// <returns>The created or reused popover handle.</returns>
    Task<INTPopoverHandle> OpenAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(NTPopoverOptions? options = null, IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : IComponent;

    /// <summary>
    ///     Shows the specified popover.
    /// </summary>
    /// <param name="popover">The popover to show.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowAsync(INTPopoverHandle popover);

    /// <summary>
    ///     Updates the position of the specified popover.
    /// </summary>
    /// <param name="popover">The popover to move.</param>
    /// <param name="left">The new left position in pixels.</param>
    /// <param name="top">The new top position in pixels.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdatePositionAsync(INTPopoverHandle popover, double left, double top);
}
