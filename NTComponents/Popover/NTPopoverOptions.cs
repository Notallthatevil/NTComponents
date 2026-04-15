using System.Diagnostics.CodeAnalysis;

namespace NTComponents;

/// <summary>
///     Represents configuration options for a floating popover window.
/// </summary>
[ExcludeFromCodeCoverage]
public class NTPopoverOptions {

    /// <summary>
    ///     Gets the background color of the popover surface.
    /// </summary>
    public TnTColor BackgroundColor { get; init; } = TnTColor.SurfaceContainerHigh;

    /// <summary>
    ///     Gets a value indicating whether the popover can be dragged by its title bar.
    /// </summary>
    public bool AllowDragging { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether pressing Escape closes the popover.
    /// </summary>
    public bool CloseOnEscape { get; init; } = true;

    /// <summary>
    ///     Gets an optional description announced to assistive technologies.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Gets an optional custom CSS class for the popover element.
    /// </summary>
    public string? ElementClass { get; init; }

    /// <summary>
    ///     Gets optional additional inline styles for the popover element.
    /// </summary>
    public string? ElementStyle { get; init; }

    /// <summary>
    ///     Gets the desired height of the popover.
    /// </summary>
    public string Height { get; init; } = "auto";

    /// <summary>
    ///     Gets the initial left position of the popover in pixels.
    /// </summary>
    public double? InitialLeft { get; init; }

    /// <summary>
    ///     Gets the initial top position of the popover in pixels.
    /// </summary>
    public double? InitialTop { get; init; }

    /// <summary>
    ///     Gets the maximum height of the popover.
    /// </summary>
    public string MaxHeight { get; init; } = "min(calc(100vh - 96px), 672px)";

    /// <summary>
    ///     Gets the maximum width of the popover.
    /// </summary>
    public string MaxWidth { get; init; } = "min(calc(100vw - 32px), 672px)";

    /// <summary>
    ///     Gets an optional stable key used to reuse an existing popover instead of creating a duplicate.
    /// </summary>
    public string? InstanceKey { get; init; }

    /// <summary>
    ///     Gets the minimum height of the popover.
    /// </summary>
    public string MinHeight { get; init; } = "192px";

    /// <summary>
    ///     Gets the minimum width of the popover.
    /// </summary>
    public string MinWidth { get; init; } = "288px";

    /// <summary>
    ///     Gets a value indicating whether a close button is shown in the header.
    /// </summary>
    public bool ShowCloseButton { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether a hide button is shown in the header.
    /// </summary>
    public bool ShowHideButton { get; init; } = true;

    /// <summary>
    ///     Gets the foreground color of the popover surface.
    /// </summary>
    public TnTColor TextColor { get; init; } = TnTColor.OnSurface;

    /// <summary>
    ///     Gets the title shown in the popover header and hidden-window launcher.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the desired width of the popover.
    /// </summary>
    public string Width { get; init; } = "min(512px, calc(100vw - 32px))";
}
