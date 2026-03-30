namespace NTComponents;

/// <summary>
///     Represents a single breadcrumb item rendered by <see cref="TnTBreadcrumbs" />.
/// </summary>
public sealed class TnTBreadcrumbItem {

    /// <summary>
    ///     Gets or sets the accessible label for the breadcrumb item.
    /// </summary>
    /// <remarks>
    ///     This should be provided for icon-only items so the item has an accessible name.
    /// </remarks>
    public string? AriaLabel { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the breadcrumb item is disabled.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>
    ///     Gets or sets the navigation target for the breadcrumb item.
    /// </summary>
    public string? Href { get; init; }

    /// <summary>
    ///     Gets or sets the icon displayed before the breadcrumb label.
    /// </summary>
    public TnTIcon? Icon { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether this item represents the current page.
    /// </summary>
    public bool IsCurrent { get; init; }

    /// <summary>
    ///     Gets or sets the visible text for the breadcrumb item.
    /// </summary>
    public string? Text { get; init; }
}
