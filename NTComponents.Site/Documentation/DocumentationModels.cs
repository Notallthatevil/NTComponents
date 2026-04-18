using NTComponents.GeneratedDocumentation;

namespace NTComponents.Site.Documentation;

/// <summary>
/// Describes a documentation group shown in the site navigation.
/// </summary>
/// <typeparam name="TEntry">The grouped entry type.</typeparam>
/// <param name="Name">The group display name.</param>
/// <param name="Slug">The stable group slug.</param>
/// <param name="Entries">The entries in the group.</param>
public sealed record DocumentationGroup<TEntry>(string Name, string Slug, IReadOnlyList<TEntry> Entries);

/// <summary>
/// Describes curated metadata for a component documentation page.
/// </summary>
/// <param name="TypeName">The generated type name without generic arguments.</param>
/// <param name="DisplayName">The display name shown in documentation navigation.</param>
/// <param name="Category">The component documentation category.</param>
/// <param name="Summary">The human-written summary for the component.</param>
/// <param name="IsFeatured">Whether the component should be highlighted on landing pages.</param>
/// <param name="Keywords">Additional search keywords.</param>
/// <param name="Examples">Curated examples for the component.</param>
public sealed record CuratedComponentDocumentation(
    string TypeName,
    string DisplayName,
    string Category,
    string Summary,
    bool IsFeatured,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<ComponentExampleDocumentation> Examples);

/// <summary>
/// Describes a curated component example.
/// </summary>
/// <param name="Title">The example title.</param>
/// <param name="Summary">The example summary.</param>
/// <param name="Code">The Razor code snippet.</param>
/// <param name="RuntimeOptions">Options the future demo can expose at runtime.</param>
public sealed record ComponentExampleDocumentation(
    string Title,
    string Summary,
    string Code,
    IReadOnlyList<ComponentRuntimeOptionDocumentation> RuntimeOptions);

/// <summary>
/// Describes one runtime configuration control for a component example.
/// </summary>
/// <param name="Name">The option name.</param>
/// <param name="Kind">The expected control kind, such as Toggle, Select, Text, or Number.</param>
/// <param name="Values">Known values for finite options.</param>
public sealed record ComponentRuntimeOptionDocumentation(string Name, string Kind, IReadOnlyList<string> Values);

/// <summary>
/// Wraps generated documentation and curated metadata for a component.
/// </summary>
/// <param name="Slug">The stable component route slug.</param>
/// <param name="TypeName">The component type name without generic arguments.</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="Category">The component category.</param>
/// <param name="Summary">The best available summary.</param>
/// <param name="Namespace">The generated API namespace, when available.</param>
/// <param name="IsFeatured">Whether the component should be highlighted.</param>
/// <param name="ApiType">The generated API documentation for the component, when available.</param>
/// <param name="Examples">Curated component examples.</param>
/// <param name="Keywords">Search keywords.</param>
public sealed record ComponentDocumentationEntry(
    string Slug,
    string TypeName,
    string DisplayName,
    string Category,
    string Summary,
    string Namespace,
    bool IsFeatured,
    TypeDocumentation? ApiType,
    IReadOnlyList<ComponentExampleDocumentation> Examples,
    IReadOnlyList<string> Keywords);

/// <summary>
/// Wraps a generated type documentation entry for API browsing.
/// </summary>
/// <param name="Slug">The stable API route slug.</param>
/// <param name="TypeName">The type name without generic arguments.</param>
/// <param name="DisplayName">The generated display name.</param>
/// <param name="Namespace">The generated namespace.</param>
/// <param name="Category">The inferred documentation category.</param>
/// <param name="Kind">The generated type kind.</param>
/// <param name="Summary">The generated summary.</param>
/// <param name="Type">The generated type documentation.</param>
public sealed record ApiDocumentationEntry(
    string Slug,
    string TypeName,
    string DisplayName,
    string Namespace,
    string Category,
    string Kind,
    string Summary,
    TypeDocumentation Type);

/// <summary>
/// Describes one documentation search result.
/// </summary>
/// <param name="Kind">The result kind.</param>
/// <param name="Slug">The route slug.</param>
/// <param name="Title">The result title.</param>
/// <param name="Summary">The result summary.</param>
/// <param name="Category">The result category.</param>
/// <param name="Namespace">The result namespace.</param>
public sealed record DocumentationSearchResult(
    DocumentationSearchResultKind Kind,
    string Slug,
    string Title,
    string Summary,
    string Category,
    string Namespace);

/// <summary>
/// Identifies the target documentation area for a search result.
/// </summary>
public enum DocumentationSearchResultKind {
    /// <summary>
    /// A curated component documentation page.
    /// </summary>
    Component,

    /// <summary>
    /// A generated API documentation page.
    /// </summary>
    Api,
}
