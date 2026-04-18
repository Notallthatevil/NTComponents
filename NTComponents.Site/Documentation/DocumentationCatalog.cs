using NTComponents.GeneratedDocumentation;

namespace NTComponents.Site.Documentation;

/// <summary>
/// Builds searchable documentation indexes from generated API documentation and curated component metadata.
/// </summary>
public sealed class DocumentationCatalog {

    private readonly Dictionary<string, ComponentDocumentationEntry> _componentsBySlug;
    private readonly Dictionary<string, ApiDocumentationEntry> _apiTypesBySlug;
    private readonly HashSet<string> _componentTypeNames;

    /// <summary>
    /// Creates a new catalog from the generated documentation model.
    /// </summary>
    /// <param name="model">The generated code documentation model.</param>
    public DocumentationCatalog(CodeDocumentationModel model) {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        ApiTypes = CreateApiEntries(model).ToArray();
        ApiGroups = GroupByCategory(ApiTypes).ToArray();
        Components = CreateComponentEntries(ApiTypes).ToArray();
        ComponentGroups = GroupByCategory(Components).ToArray();
        _componentsBySlug = Components.ToDictionary(static component => component.Slug, StringComparer.OrdinalIgnoreCase);
        _apiTypesBySlug = ApiTypes.ToDictionary(static entry => entry.Slug, StringComparer.OrdinalIgnoreCase);
        _componentTypeNames = Components.Select(static component => component.TypeName).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the default catalog backed by <see cref="GeneratedCodeDocumentation.Model" />.
    /// </summary>
    public static DocumentationCatalog Default { get; } = new(GeneratedCodeDocumentation.Model);

    /// <summary>
    /// Gets the generated documentation model wrapped by this catalog.
    /// </summary>
    public CodeDocumentationModel Model { get; }

    /// <summary>
    /// Gets component documentation entries.
    /// </summary>
    public IReadOnlyList<ComponentDocumentationEntry> Components { get; }

    /// <summary>
    /// Gets component documentation entries grouped by category.
    /// </summary>
    public IReadOnlyList<DocumentationGroup<ComponentDocumentationEntry>> ComponentGroups { get; }

    /// <summary>
    /// Gets generated API documentation entries.
    /// </summary>
    public IReadOnlyList<ApiDocumentationEntry> ApiTypes { get; }

    /// <summary>
    /// Gets generated API documentation entries grouped by inferred category.
    /// </summary>
    public IReadOnlyList<DocumentationGroup<ApiDocumentationEntry>> ApiGroups { get; }

    /// <summary>
    /// Gets a component documentation entry by slug.
    /// </summary>
    /// <param name="slug">The component slug.</param>
    /// <returns>The matching component entry, or <see langword="null" />.</returns>
    public ComponentDocumentationEntry? GetComponentBySlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? null : _componentsBySlug.GetValueOrDefault(slug);

    /// <summary>
    /// Gets an API documentation entry by slug.
    /// </summary>
    /// <param name="slug">The API slug.</param>
    /// <returns>The matching API entry, or <see langword="null" />.</returns>
    public ApiDocumentationEntry? GetApiTypeBySlug(string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? null : _apiTypesBySlug.GetValueOrDefault(slug);

    /// <summary>
    /// Searches component and API documentation by name, summary, namespace, category, and curated keywords.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">The maximum result count.</param>
    /// <returns>Ordered matching search results.</returns>
    public IReadOnlyList<DocumentationSearchResult> Search(string? query, int maxResults = 25) {
        if (string.IsNullOrWhiteSpace(query) || maxResults <= 0) {
            return [];
        }

        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static term => term.ToLowerInvariant())
            .ToArray();

        if (terms.Length == 0) {
            return [];
        }

        var componentResults = Components
            .Select(component => new {
                Score = ScoreComponent(component, terms),
                Result = new DocumentationSearchResult(
                    DocumentationSearchResultKind.Component,
                    component.Slug,
                    component.DisplayName,
                    component.Summary,
                    component.Category,
                    component.Namespace),
            })
            .Where(static result => result.Score > 0);

        var apiResults = ApiTypes
            .Where(api => !_componentTypeNames.Contains(api.TypeName))
            .Select(api => new {
                Score = ScoreApi(api, terms),
                Result = new DocumentationSearchResult(
                    DocumentationSearchResultKind.Api,
                    api.Slug,
                    api.DisplayName,
                    api.Summary,
                    api.Category,
                    api.Namespace),
            })
            .Where(static result => result.Score > 0);

        return componentResults
            .Concat(apiResults)
            .OrderByDescending(static result => result.Score)
            .ThenBy(static result => result.Result.Title, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .Select(static result => result.Result)
            .ToArray();
    }

    private static IEnumerable<ApiDocumentationEntry> CreateApiEntries(CodeDocumentationModel model) {
        foreach (var type in model.Types) {
            var typeName = GetSimpleTypeName(type.Name);
            var @namespace = GetNamespace(type.FullName, type.Name);
            var category = GetCategory(@namespace, typeName);

            yield return new ApiDocumentationEntry(
                DocumentationSlugs.Create(type.FullName),
                typeName,
                type.Name,
                @namespace,
                category,
                type.Kind,
                type.Summary,
                type);
        }
    }

    private static IEnumerable<ComponentDocumentationEntry> CreateComponentEntries(IReadOnlyList<ApiDocumentationEntry> apiTypes) {
        var apiByTypeName = apiTypes
            .GroupBy(static api => api.TypeName, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        var curatedByTypeName = CuratedComponentDocumentationCatalog.Components
            .ToDictionary(static component => component.TypeName, StringComparer.Ordinal);

        foreach (var typeName in CuratedComponentDocumentationCatalog.ComponentTypeNames) {
            apiByTypeName.TryGetValue(typeName, out var apiType);

            var curated = curatedByTypeName.TryGetValue(typeName, out var curatedComponent)
                ? curatedComponent
                : CuratedComponentDocumentationCatalog.CreateFallback(typeName, apiType?.Category, apiType?.Summary);

            var summary = string.IsNullOrWhiteSpace(curated.Summary) ? apiType?.Summary ?? string.Empty : curated.Summary;
            var @namespace = apiType?.Namespace ?? string.Empty;

            yield return new ComponentDocumentationEntry(
                DocumentationSlugs.Create(curated.TypeName),
                curated.TypeName,
                curated.DisplayName,
                curated.Category,
                summary,
                @namespace,
                curated.IsFeatured,
                apiType?.Type,
                curated.Examples,
                curated.Keywords);
        }
    }

    private static IEnumerable<DocumentationGroup<TEntry>> GroupByCategory<TEntry>(IReadOnlyList<TEntry> entries)
        where TEntry : notnull {
        return entries
            .GroupBy(GetEntryCategory)
            .OrderBy(static group => GetCategorySortOrder(group.Key))
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new DocumentationGroup<TEntry>(
                group.Key,
                DocumentationSlugs.Create(group.Key),
                group.OrderBy(GetEntryTitle, StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private static string GetEntryCategory<TEntry>(TEntry entry) =>
        entry switch {
            ComponentDocumentationEntry component => component.Category,
            ApiDocumentationEntry api => api.Category,
            _ => "Other",
        };

    private static string GetEntryTitle<TEntry>(TEntry entry) =>
        entry switch {
            ComponentDocumentationEntry component => component.DisplayName,
            ApiDocumentationEntry api => api.DisplayName,
            _ => string.Empty,
        };

    private static int GetCategorySortOrder(string category) =>
        category switch {
            "Buttons" => 0,
            "Layout and navigation" => 1,
            "Forms" => 2,
            "Feedback and display" => 3,
            "Data" => 4,
            "Advanced" => 5,
            "Theming" => 6,
            "Core" => 7,
            _ => 100,
        };

    private static int ScoreComponent(ComponentDocumentationEntry component, IReadOnlyList<string> terms) {
        var score = 0;
        foreach (var term in terms) {
            score += ScoreText(component.TypeName, term, 12);
            score += ScoreText(component.DisplayName, term, 12);
            score += ScoreText(component.Category, term, 8);
            score += ScoreText(component.Namespace, term, 5);
            score += ScoreText(component.Summary, term, 3);
            score += component.Keywords.Any(keyword => Contains(keyword, term)) ? 6 : 0;
        }

        return score;
    }

    private static int ScoreApi(ApiDocumentationEntry api, IReadOnlyList<string> terms) {
        var score = 0;
        foreach (var term in terms) {
            score += ScoreText(api.TypeName, term, 10);
            score += ScoreText(api.DisplayName, term, 10);
            score += ScoreText(api.Category, term, 7);
            score += ScoreText(api.Namespace, term, 5);
            score += ScoreText(api.Summary, term, 3);
            score += ScoreText(api.Kind, term, 2);
        }

        return score;
    }

    private static int ScoreText(string value, string term, int exactWeight) {
        if (string.IsNullOrWhiteSpace(value)) {
            return 0;
        }

        if (string.Equals(value, term, StringComparison.OrdinalIgnoreCase)) {
            return exactWeight * 2;
        }

        return Contains(value, term) ? exactWeight : 0;
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string GetSimpleTypeName(string typeName) {
        var genericIndex = typeName.IndexOf('<', StringComparison.Ordinal);
        return genericIndex < 0 ? typeName : typeName[..genericIndex];
    }

    private static string GetNamespace(string fullName, string displayName) {
        var plainDisplayName = GetSimpleTypeName(displayName);
        var genericIndex = fullName.IndexOf('<', StringComparison.Ordinal);
        var searchableFullName = genericIndex < 0 ? fullName : fullName[..genericIndex];
        var suffix = "." + plainDisplayName;

        if (!searchableFullName.EndsWith(suffix, StringComparison.Ordinal)) {
            var fallbackIndex = searchableFullName.LastIndexOf('.');
            return fallbackIndex < 0 ? string.Empty : searchableFullName[..fallbackIndex];
        }

        return searchableFullName[..^suffix.Length];
    }

    private static string GetCategory(string @namespace, string typeName) {
        if (@namespace.Contains(".Buttons", StringComparison.OrdinalIgnoreCase)) {
            return "Buttons";
        }

        if (@namespace.Contains(".Layout", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Nav", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Header", StringComparison.OrdinalIgnoreCase)) {
            return "Layout and navigation";
        }

        if (@namespace.Contains(".Form", StringComparison.OrdinalIgnoreCase) ||
            typeName.StartsWith("TnTInput", StringComparison.Ordinal)) {
            return "Forms";
        }

        if (@namespace.Contains(".Grid", StringComparison.OrdinalIgnoreCase)) {
            return "Data";
        }

        if (@namespace.Contains(".Theming", StringComparison.OrdinalIgnoreCase)) {
            return "Theming";
        }

        if (@namespace.Contains(".Toast", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".Tooltip", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".Badge", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".Cards", StringComparison.OrdinalIgnoreCase)) {
            return "Feedback and display";
        }

        if (@namespace.Contains(".Dialog", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".TabView", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".Accordion", StringComparison.OrdinalIgnoreCase) ||
            @namespace.Contains(".Editors", StringComparison.OrdinalIgnoreCase)) {
            return "Advanced";
        }

        return "Core";
    }
}
