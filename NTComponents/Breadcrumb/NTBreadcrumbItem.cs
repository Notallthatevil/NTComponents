namespace NTComponents;

/// <summary>Describes one level of a breadcrumb trail.</summary>
/// <param name="Label">The visible text for this level.</param>
/// <param name="Href">The ancestor destination, or null for a text-only level. Ignored for the final item.</param>
public sealed record NTBreadcrumbItem(string Label, string? Href = null);
