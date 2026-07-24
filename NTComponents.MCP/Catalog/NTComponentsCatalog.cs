using System.Text;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using NTComponents.GeneratedDocumentation;
using NTComponents.MCP.Contracts;

namespace NTComponents.MCP.Catalog;

public sealed class NTComponentsCatalog {
    private const string DocumentationBaseUrl = "https://ntcomponents.nttechnologies.dev";
    private const int MaximumInlineEnumValues = 20;
    private static readonly HashSet<string> ComponentInfrastructureMethodNames = [
        "Attach",
        "BuildRenderTree",
        "Dispose",
        "DisposeAsync",
        "OnAfterRender",
        "OnAfterRenderAsync",
        "OnInitialized",
        "OnInitializedAsync",
        "OnParametersSet",
        "OnParametersSetAsync",
        "SetParametersAsync",
        "ShouldRender",
    ];
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private readonly IReadOnlyList<TypeDocumentation> _components;
    private readonly IReadOnlyList<TypeDocumentation> _references;
    private readonly IReadOnlyDictionary<string, TypeDocumentation> _componentsByName;
    private readonly IReadOnlyDictionary<string, TypeDocumentation> _referencesByName;

    public NTComponentsCatalog() {
        var documentedTypes = GeneratedCodeDocumentation.Model.Types;
        var publicComponentNames = typeof(NTButton).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .Select(NormalizeRuntimeFullName)
            .ToHashSet(StringComparer.Ordinal);

        _components = documentedTypes
            .Where(type => publicComponentNames.Contains(NormalizeDocumentationFullName(type.FullName)))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

        _references = documentedTypes
            .Where(type => string.Equals(type.Accessibility, "Public", StringComparison.OrdinalIgnoreCase))
            .Where(type => !_components.Contains(type))
            .Where(type => type.Kind == "Enum" || type.Name.StartsWith("NT", StringComparison.Ordinal) || _components.Any(component => UsesType(component, type)))
            .OrderBy(GetReferenceKind, StringComparer.Ordinal)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

        _componentsByName = BuildLookup(_components);
        _referencesByName = BuildLookup(_references);
    }

    public CatalogOverview GetOverview() {
        var serverVersion = GetAssemblyVersion(typeof(NTComponentsCatalog).Assembly);
        var componentsVersion = GetAssemblyVersion(typeof(NTButton).Assembly);
        return new(
            serverVersion,
            componentsVersion,
            GetBuildRevision(serverVersion),
            DocumentationBaseUrl,
            _components.Count,
            _references.Count,
            _components.Select(component => component.SourceFolder).Where(folder => !string.IsNullOrWhiteSpace(folder)).Distinct(StringComparer.Ordinal).OrderBy(folder => folder, StringComparer.Ordinal).ToArray(),
            _references.Select(GetReferenceKind).Distinct(StringComparer.Ordinal).OrderBy(kind => kind, StringComparer.Ordinal).ToArray(),
            [CatalogInputValidator.ComponentApiReferenceScope, CatalogInputValidator.LibraryApiReferenceScope]);
    }

    public IReadOnlyList<ComponentSummary> ListComponents(string? query = null, string? folder = null, bool includeObsolete = false, int limit = 100) {
        return ListComponentPage(query, folder, includeObsolete, limit).Items;
    }

    public CatalogPage<ComponentSummary> ListComponentPage(string? query = null, string? folder = null, bool includeObsolete = false, int limit = 100, int offset = 0) {
        CatalogInputValidator.ValidateLimit(limit);
        CatalogInputValidator.ValidateOffset(offset);
        CatalogInputValidator.ValidateOptionalQuery(query);
        SearchQuery? searchQuery = string.IsNullOrWhiteSpace(query) ? null : CreateSearchQuery(query);
        var matches = _components
            .Where(component => includeObsolete || !component.IsObsolete)
            .Where(component => string.IsNullOrWhiteSpace(folder) || string.Equals(component.SourceFolder, folder, StringComparison.OrdinalIgnoreCase))
            .Select(component => (Component: component, Match: searchQuery is null ? SearchMatch.Empty : CalculateMatch(component, searchQuery.Value, isComponent: true)))
            .Where(result => searchQuery is null || result.Match.Score > 0)
            .OrderByDescending(result => result.Match.Score)
            .ThenBy(result => result.Component.Name, StringComparer.Ordinal)
            .ToArray();
        if (searchQuery is { } componentQuery && matches.Any(result => result.Match.MatchedTerms.Count == componentQuery.Terms.Length)) {
            matches = matches.Where(result => result.Match.MatchedTerms.Count == componentQuery.Terms.Length).ToArray();
        }

        return CreatePage(matches
            .Skip(offset)
            .Take(limit)
            .Select(result => result.Component)
            .Select(ToComponentSummary)
            .ToArray(), matches.Length, offset, limit);
    }

    public ComponentDetails? GetComponent(string name, bool includeRelatedEnumValues = false) {
        CatalogInputValidator.ValidateRequiredText(name, nameof(name));
        return TryFind(_componentsByName, name, out var component) ? ToComponentDetails(component, includeRelatedEnumValues) : null;
    }

    public IReadOnlyList<ReferenceSummary> ListReferences(string? query = null, string? kind = null, bool includeObsolete = false, int limit = 100) {
        return ListReferencePage(query, kind, null, includeObsolete, limit).Items;
    }

    public CatalogPage<ReferenceSummary> ListReferencePage(string? query = null, string? kind = null, string? scope = null, bool includeObsolete = false, int limit = 100, int offset = 0) {
        CatalogInputValidator.ValidateLimit(limit);
        CatalogInputValidator.ValidateOffset(offset);
        CatalogInputValidator.ValidateOptionalQuery(query);
        CatalogInputValidator.ValidateReferenceKind(kind);
        CatalogInputValidator.ValidateReferenceScope(scope);
        SearchQuery? searchQuery = string.IsNullOrWhiteSpace(query) ? null : CreateSearchQuery(query);
        var matches = _references
            .Where(reference => includeObsolete || !reference.IsObsolete)
            .Where(reference => kind is null || string.Equals(GetReferenceKind(reference), kind, StringComparison.OrdinalIgnoreCase))
            .Where(reference => scope is null || string.Equals(GetReferenceScope(reference), scope, StringComparison.OrdinalIgnoreCase))
            .Select(reference => (Reference: reference, Match: searchQuery is null ? SearchMatch.Empty : CalculateMatch(reference, searchQuery.Value, isComponent: false)))
            .Where(result => searchQuery is null || result.Match.Score > 0)
            .OrderByDescending(result => result.Match.Score)
            .ThenBy(result => result.Reference.Name, StringComparer.Ordinal)
            .ToArray();
        if (searchQuery is { } referenceQuery && matches.Any(result => result.Match.MatchedTerms.Count == referenceQuery.Terms.Length)) {
            matches = matches.Where(result => result.Match.MatchedTerms.Count == referenceQuery.Terms.Length).ToArray();
        }

        return CreatePage(matches
            .Skip(offset)
            .Take(limit)
            .Select(result => result.Reference)
            .Select(ToReferenceSummary)
            .ToArray(), matches.Length, offset, limit);
    }

    public ReferenceDetails? GetReference(string name) {
        CatalogInputValidator.ValidateRequiredText(name, nameof(name));
        return TryFind(_referencesByName, name, out var reference) ? ToReferenceDetails(reference) : null;
    }

    public IReadOnlyList<DocumentationSearchResult> Search(string query, int limit = 25) {
        return SearchPage(query, limit).Items;
    }

    public DocumentationSearchPage SearchPage(string query, int limit = 25, int offset = 0) {
        CatalogInputValidator.ValidateRequiredQuery(query);
        CatalogInputValidator.ValidateLimit(limit);
        CatalogInputValidator.ValidateOffset(offset);
        var searchQuery = CreateSearchQuery(query);
        var results = new List<DocumentationSearchResult>();
        foreach (var component in _components) {
            var match = CalculateMatch(component, searchQuery, isComponent: true);
            if (match.Score > 0) {
                results.Add(new(component.Name, component.FullName, "Component", component.Summary, component.SourceFolder, match.Score, match.MatchedTerms, match.MatchedFields, GetComponentDocumentationUrl(component)));
            }
        }

        foreach (var reference in _references) {
            var match = CalculateMatch(reference, searchQuery, isComponent: false);
            if (match.Score > 0) {
                results.Add(new(reference.Name, reference.FullName, GetReferenceKind(reference), reference.Summary, reference.SourceFolder, match.Score, match.MatchedTerms, match.MatchedFields, GetReferenceDocumentationUrl(reference)));
            }
        }

        if (results.Any(result => result.MatchedTerms.Count == searchQuery.Terms.Length)) {
            results.RemoveAll(result => result.MatchedTerms.Count != searchQuery.Terms.Length);
        }

        var orderedResults = results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Name, StringComparer.Ordinal)
            .ToArray();
        var page = CreatePage(orderedResults.Skip(offset).Take(limit).ToArray(), orderedResults.Length, offset, limit);
        return new(page.Items, page.TotalCount, page.Offset, page.Limit, page.HasMore, page.NextOffset, orderedResults.Length == 0 ? GetSuggestedQuery(searchQuery) : null);
    }

    private ComponentDetails ToComponentDetails(TypeDocumentation component, bool includeRelatedEnumValues) {
        var relatedReferences = _references.Where(reference => UsesType(component, reference)).ToArray();
        var relatedTypes = relatedReferences.Select(ToReferenceSummary).ToArray();
        var relatedComponents = _components
            .Where(candidate => candidate != component && (UsesType(component, candidate) || UsesType(candidate, component)))
            .Select(ToComponentSummary)
            .OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ToArray();
        var requiredParameters = component.Parameters.Where(parameter => parameter.IsEditorRequired).Select(parameter => parameter.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var guidelines = new List<string>();
        if (requiredParameters.Length > 0) {
            guidelines.Add($"Provide required parameters: {string.Join(", ", requiredParameters)}.");
        }
        if (!string.IsNullOrWhiteSpace(component.CompatibilitySummary)) {
            guidelines.Add(component.CompatibilitySummary);
        }
        if (!string.IsNullOrWhiteSpace(component.CompatibilityDetails)) {
            guidelines.Add(component.CompatibilityDetails);
        }
        if (component.IsObsolete) {
            guidelines.Add($"Do not use this obsolete component. {component.ObsoleteMessage}".Trim());
        }

        var razorUsage = GetRazorUsage(component);
        return new(
            component.Name,
            component.FullName,
            component.SourceFolder,
            component.SourceFileName,
            component.Summary,
            component.Remarks,
            component.RenderCompatibility,
            component.IsSsrCompatible,
            component.CompatibilitySummary,
            component.CompatibilityDetails,
            component.IsObsolete,
            component.ObsoleteMessage,
            component.Parameters.Where(parameter => IsPublicOrProtected(parameter.Accessibility)).Select(ToParameterDetails).OrderBy(parameter => parameter.CategoryOrder).ThenBy(parameter => parameter.Name, StringComparer.Ordinal).ToArray(),
            component.Methods.Where(IsConsumerMethod).Select(ToMemberDetails).ToArray(),
            relatedTypes,
            relatedComponents,
            guidelines,
            razorUsage,
            [new UsageExample("Basic usage", "A minimal example using the component's documented composition and required inputs.", razorUsage)],
            GetComponentDocumentationUrl(component),
            includeRelatedEnumValues ? relatedReferences.Where(reference => reference.Kind == "Enum").Select(ToRelatedEnumDetails).ToArray() : []);
    }

    private ReferenceDetails ToReferenceDetails(TypeDocumentation reference) => new(
        reference.Name,
        reference.FullName,
        GetReferenceKind(reference),
        reference.SourceFolder,
        reference.SourceFileName,
        reference.Summary,
        reference.Remarks,
        reference.IsObsolete,
        reference.ObsoleteMessage,
        reference.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility)).Select(ToFieldDetails).ToArray(),
        reference.Properties.Where(property => !property.IsFromBaseType && IsPublicOrProtected(property.Accessibility)).Select(ToParameterDetails).OrderBy(property => property.CategoryOrder).ThenBy(property => property.Name, StringComparer.Ordinal).ToArray(),
        reference.Methods.Where(method => !method.IsFromBaseType && IsPublicOrProtected(method.Accessibility)).Select(ToMemberDetails).ToArray(),
        GetUsingComponents(reference),
        GetReferenceScope(reference),
        GetReferenceDocumentationUrl(reference));

    private ComponentSummary ToComponentSummary(TypeDocumentation component) => new(
        component.Name,
        component.FullName,
        component.SourceFolder,
        component.Summary,
        component.RenderCompatibility,
        component.IsObsolete,
        component.Parameters.Where(parameter => parameter.IsEditorRequired).Select(parameter => parameter.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
        GetComponentDocumentationUrl(component));

    private ReferenceSummary ToReferenceSummary(TypeDocumentation reference) => new(reference.Name, reference.FullName, GetReferenceKind(reference), reference.Summary, reference.IsObsolete, GetUsingComponents(reference), GetReferenceScope(reference), GetReferenceDocumentationUrl(reference));

    private IReadOnlyList<string> GetUsingComponents(TypeDocumentation reference) => _components.Where(component => UsesType(component, reference)).Select(component => component.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private RelatedEnumDetails ToRelatedEnumDetails(TypeDocumentation reference) {
        var values = reference.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility)).ToArray();
        return new(reference.Name, reference.FullName, values.Take(MaximumInlineEnumValues).Select(ToFieldDetails).ToArray(), values.Length, values.Length > MaximumInlineEnumValues);
    }

    private static ParameterDetails ToParameterDetails(PropertyDocumentation parameter) {
        var (category, categoryOrder) = GetParameterCategory(parameter);
        return new(
            parameter.Name,
            parameter.TypeDisplayName,
            parameter.Summary,
            parameter.IsEditorRequired,
            parameter.IsCascadingParameter,
            parameter.IsFromBaseType,
            parameter.IsObsolete,
            parameter.Accessibility,
            string.IsNullOrEmpty(parameter.DefaultValueExpression) ? null : parameter.DefaultValueExpression,
            category,
            categoryOrder);
    }

    private static MemberDetails ToMemberDetails(MethodDocumentation method) => new(method.Name, method.Signature, method.Summary, method.IsFromBaseType, method.IsObsolete, method.Accessibility);

    private static FieldDetails ToFieldDetails(FieldDocumentation field) => new(field.Name, field.TypeDisplayName, field.ConstantValue, field.Summary, field.IsObsolete);

    private static bool IsConsumerMethod(MethodDocumentation method) =>
        !method.IsFromBaseType && IsPublicOrProtected(method.Accessibility) && !ComponentInfrastructureMethodNames.Contains(method.Name);

    private static string GetRazorUsage(TypeDocumentation component) {
        var documentedExample = GetDocumentedExample(component.XmlDocumentation);
        if (!string.IsNullOrWhiteSpace(documentedExample)) {
            return documentedExample;
        }

        var requiredParameters = component.Parameters.Where(parameter => parameter.IsEditorRequired && IsPublicOrProtected(parameter.Accessibility)).ToArray();
        var tagName = NormalizeDocumentationName(component.Name);
        return requiredParameters.Length == 0
            ? $"<{tagName} />"
            : $"<{tagName} {string.Join(" ", requiredParameters.Select(parameter => $"{parameter.Name}=\"{GetRequiredParameterPlaceholder(parameter)}\""))} />";
    }

    private static string GetDocumentedExample(string xmlDocumentation) {
        if (string.IsNullOrWhiteSpace(xmlDocumentation)) {
            return string.Empty;
        }

        try {
            var document = XDocument.Parse("<root>" + xmlDocumentation + "</root>");
            var example = document.Root?.Descendants("example").FirstOrDefault();
            return (example?.Descendants("code").FirstOrDefault()?.Value ?? example?.Value ?? string.Empty).Trim();
        }
        catch {
            return string.Empty;
        }
    }

    private static string GetRequiredParameterPlaceholder(PropertyDocumentation parameter) {
        if (parameter.TypeDisplayName.StartsWith("string", StringComparison.Ordinal)) {
            return "TODO";
        }

        if (parameter.TypeDisplayName is "bool" or "bool?") {
            return "true";
        }

        if (parameter.TypeDisplayName is "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" or "float" or "double" or "decimal") {
            return "0";
        }

        return "@" + char.ToLowerInvariant(parameter.Name[0]) + parameter.Name[1..];
    }

    private static SearchMatch CalculateMatch(TypeDocumentation type, SearchQuery query, bool isComponent) {
        if (string.Equals(type.Name, query.Text, StringComparison.OrdinalIgnoreCase) || string.Equals(type.FullName, query.Text, StringComparison.OrdinalIgnoreCase)) {
            return new(1000, query.Terms, ["Name"]);
        }

        if (query.Terms.Length == 0) {
            return SearchMatch.Empty;
        }

        var score = 0;
        var matchedTerms = new List<string>();
        var matchedFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var term in query.Terms) {
            var termMatch = CalculateTermMatch(type, term, isComponent);
            if (termMatch.Score == 0) {
                continue;
            }

            score += termMatch.Score;
            matchedTerms.Add(term);
            matchedFields.UnionWith(termMatch.Fields);
        }

        return new(matchedTerms.Count == query.Terms.Length ? score + 100 : score, matchedTerms, matchedFields.OrderBy(field => field, StringComparer.Ordinal).ToArray());
    }

    private static TermMatch CalculateTermMatch(TypeDocumentation type, string term, bool isComponent) {
        var score = 0;
        var fields = new HashSet<string>(StringComparer.Ordinal);
        ConsiderMatch(ref score, fields, "Name", Math.Max(ScoreName(type.Name, term, 120, 80), ScoreName(type.FullName, term, 110, 70)));
        ConsiderMatch(ref score, fields, "Folder", ScoreText(type.SourceFolder, term, 35, 20));
        ConsiderMatch(ref score, fields, "Summary", ContainsTerm(type.Summary, term) ? 55 : 0);
        ConsiderMatch(ref score, fields, "Remarks", ContainsTerm(type.Remarks, term) ? 20 : 0);

        if (!string.Equals(type.RenderCompatibility, "Unknown", StringComparison.Ordinal)) {
            ConsiderMatch(ref score, fields, "RenderCompatibility", ContainsTerm(nameof(type.RenderCompatibility), term) ? 50 : 0);
            ConsiderMatch(ref score, fields, "RenderCompatibility", ScoreText(type.RenderCompatibility, term, 45, 40));
            ConsiderMatch(ref score, fields, "RenderCompatibility", ContainsTerm(type.CompatibilitySummary, term) || ContainsTerm(type.CompatibilityDetails, term) ? 25 : 0);
        }

        foreach (var property in type.Properties.Where(property => IsPublicOrProtected(property.Accessibility))) {
            ConsiderMatch(ref score, fields, "Parameter.Name", ScoreText(property.Name, term, 75, 55));
            ConsiderMatch(ref score, fields, "Parameter.TypeOrSummary", ContainsTerm(property.Signature, term) || ContainsTerm(property.TypeDisplayName, term) || ContainsTerm(property.TypeFullName, term) || ContainsTerm(property.Summary, term) ? 25 : 0);
        }

        foreach (var method in type.Methods) {
            if (isComponent ? !IsConsumerMethod(method) : method.IsFromBaseType || !IsPublicOrProtected(method.Accessibility)) {
                continue;
            }

            ConsiderMatch(ref score, fields, "Method.Name", ScoreText(method.Name, term, 75, 55));
            ConsiderMatch(ref score, fields, "Method.SignatureOrSummary", ContainsTerm(method.Signature, term) || ContainsTerm(method.Summary, term) ? 25 : 0);
        }

        foreach (var field in type.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility))) {
            ConsiderMatch(ref score, fields, "Field.Name", ScoreText(field.Name, term, 75, 55));
            ConsiderMatch(ref score, fields, "Field.ValueOrSummary", ContainsTerm(field.Signature, term) || ContainsTerm(field.TypeDisplayName, term) || ContainsTerm(field.TypeFullName, term) || ContainsTerm(field.ConstantValue, term) || ContainsTerm(field.Summary, term) ? 25 : 0);
        }

        return new(score, fields);
    }

    private static SearchQuery CreateSearchQuery(string query) {
        var normalizedQuery = query.Trim();
        var terms = normalizedQuery
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '<', '>'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term => term.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        CatalogInputValidator.ValidateQueryTermCount(terms.Length);
        return new(normalizedQuery, terms);
    }

    private static int ScoreText(string text, string term, int exactScore, int containsScore) =>
        string.Equals(text, term, StringComparison.OrdinalIgnoreCase) ? exactScore : ContainsTerm(text, term) ? containsScore : 0;

    private static int ScoreName(string text, string term, int exactScore, int containsScore) =>
        string.Equals(NormalizeSuggestionName(text), NormalizeSuggestionName(term), StringComparison.Ordinal) ? exactScore : ScoreText(text, term, exactScore, containsScore);

    private static bool ContainsTerm(string text, string term) {
        if (term.Length > 3) {
            return text.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        for (var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase); index >= 0; index = text.IndexOf(term, index + 1, StringComparison.OrdinalIgnoreCase)) {
            var startsWord = index == 0 || !char.IsLetterOrDigit(text[index - 1]) || char.IsLower(text[index - 1]) && char.IsUpper(text[index]);
            var endIndex = index + term.Length;
            var endsWord = endIndex == text.Length || !char.IsLetterOrDigit(text[endIndex]) || char.IsLower(text[endIndex - 1]) && char.IsUpper(text[endIndex]);
            if (startsWord && endsWord) {
                return true;
            }
        }

        return false;
    }

    private static void ConsiderMatch(ref int score, ISet<string> fields, string field, int candidateScore) {
        if (candidateScore <= 0) {
            return;
        }

        score = Math.Max(score, candidateScore);
        fields.Add(field);
    }

    private static bool UsesType(TypeDocumentation component, TypeDocumentation reference) {
        var referenceFullName = NormalizeDocumentationFullName(reference.FullName);
        var referenceName = NormalizeDocumentationName(reference.Name);
        return component.Properties.Any(property => ContainsType(property.TypeFullName, property.TypeDisplayName, referenceFullName, referenceName))
            || component.Methods.Any(method => method.Signature.Contains(referenceFullName, StringComparison.Ordinal) || method.Signature.Contains(referenceName, StringComparison.Ordinal));
    }

    private static bool ContainsType(string fullTypeName, string displayTypeName, string referenceFullName, string referenceName) =>
        fullTypeName.Contains(referenceFullName, StringComparison.Ordinal) || displayTypeName.Contains(referenceName, StringComparison.Ordinal);

    private static (string Category, int Order) GetParameterCategory(PropertyDocumentation parameter) {
        if (parameter.IsFromBaseType) {
            return ("Inherited", 7);
        }

        if (parameter.IsEditorRequired) {
            return ("Required", 0);
        }

        var name = parameter.Name;
        if (parameter.TypeDisplayName.Contains("EventCallback", StringComparison.Ordinal) || name.StartsWith("On", StringComparison.Ordinal) || name.EndsWith("Changed", StringComparison.Ordinal)) {
            return ("Events", 5);
        }

        if (ContainsAny(name, "Items", "Item", "Value", "Values", "Model", "Data", "Source", "Selected", "Selection")) {
            return ("Data", 1);
        }

        if (ContainsAny(name, "ChildContent", "Content", "Label", "Text", "Title", "Icon", "Placeholder", "Description")) {
            return ("Content", 2);
        }

        if (ContainsAny(name, "Color", "Variant", "Appearance", "Density", "Size", "Shape", "Elevation", "Class", "Style", "Width", "Height")) {
            return ("Appearance", 4);
        }

        if (ContainsAny(name, "AdditionalAttributes", "Element", "AutoFocus", "Virtual", "Overscan", "Cache", "Debounce", "Throttle")) {
            return ("Advanced", 6);
        }

        return ("Behavior", 3);
    }

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private string GetReferenceScope(TypeDocumentation reference) => GetUsingComponents(reference).Count > 0 ? CatalogInputValidator.ComponentApiReferenceScope : CatalogInputValidator.LibraryApiReferenceScope;

    private static string GetComponentDocumentationUrl(TypeDocumentation component) => $"{DocumentationBaseUrl}/components/{GetSlug(component.Name)}";

    private static string GetReferenceDocumentationUrl(TypeDocumentation reference) => $"{DocumentationBaseUrl}/reference/{(reference.Kind == "Enum" ? "enums" : "constants")}/{GetSlug(reference.FullName)}";

    private static string GetSlug(string value) {
        var slug = new StringBuilder(value.Length);
        foreach (var character in value) {
            if (char.IsLetterOrDigit(character)) {
                slug.Append(char.ToLowerInvariant(character));
            }
            else if (slug.Length > 0 && slug[^1] != '-') {
                slug.Append('-');
            }
        }

        return slug.ToString().Trim('-');
    }

    private static CatalogPage<T> CreatePage<T>(IReadOnlyList<T> items, int totalCount, int offset, int limit) {
        var nextOffset = offset + items.Count;
        var hasMore = nextOffset < totalCount;
        return new(items, totalCount, offset, limit, hasMore, hasMore ? nextOffset : null);
    }

    private string? GetSuggestedQuery(SearchQuery query) {
        if (query.Terms.Length != 1) {
            return null;
        }

        var requestedName = NormalizeSuggestionName(query.Terms[0]);
        return _components.Concat(_references)
            .Select(type => (type.Name, Distance: CalculateEditDistance(requestedName, NormalizeSuggestionName(type.Name))))
            .Where(candidate => candidate.Distance <= 3)
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Select(candidate => candidate.Name)
            .FirstOrDefault();
    }

    private static string NormalizeSuggestionName(string value) {
        var normalized = new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return normalized.StartsWith("nt", StringComparison.Ordinal) ? normalized[2..] : normalized;
    }

    private static int CalculateEditDistance(string left, string right) {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++) {
            var current = new int[right.Length + 1];
            current[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++) {
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1));
            }

            previous = current;
        }

        return previous[^1];
    }

    private static string GetAssemblyVersion(Assembly assembly) => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";

    private static string GetBuildRevision(string version) {
        var separatorIndex = version.IndexOf('+');
        return separatorIndex < 0 ? string.Empty : version[(separatorIndex + 1)..];
    }

    private static IReadOnlyDictionary<string, TypeDocumentation> BuildLookup(IEnumerable<TypeDocumentation> types) {
        var lookup = new Dictionary<string, TypeDocumentation>(NameComparer);
        foreach (var type in types) {
            lookup.TryAdd(type.Name, type);
            lookup.TryAdd(NormalizeDocumentationName(type.Name), type);
            lookup.TryAdd(type.FullName, type);
            lookup.TryAdd(NormalizeDocumentationFullName(type.FullName), type);
        }
        return lookup;
    }

    private static bool TryFind(IReadOnlyDictionary<string, TypeDocumentation> lookup, string name, out TypeDocumentation type) => lookup.TryGetValue(name.Trim(), out type!);

    private static string GetReferenceKind(TypeDocumentation type) => type.Kind == "Enum" ? "Enum" : "Helper";

    private static bool IsPublicOrProtected(string accessibility) => accessibility is "Public" or "Protected" or "ProtectedOrInternal";

    private static string NormalizeRuntimeFullName(Type type) {
        var fullName = type.FullName ?? type.Name;
        var genericIndex = fullName.IndexOf('`');
        if (genericIndex < 0) {
            return fullName.Replace('+', '.');
        }

        var normalizedName = new StringBuilder(fullName.Length);
        for (var index = 0; index < fullName.Length; index++) {
            if (fullName[index] == '`') {
                while (index + 1 < fullName.Length && char.IsAsciiDigit(fullName[index + 1])) {
                    index++;
                }
                continue;
            }

            normalizedName.Append(fullName[index] == '+' ? '.' : fullName[index]);
        }

        return normalizedName.ToString();
    }

    private static string NormalizeDocumentationFullName(string fullName) {
        var genericIndex = fullName.IndexOf('<');
        if (genericIndex < 0) {
            return fullName;
        }

        var normalizedName = new StringBuilder(fullName.Length);
        var genericDepth = 0;
        foreach (var character in fullName) {
            if (character == '<') {
                genericDepth++;
            }
            else if (character == '>') {
                genericDepth--;
            }
            else if (genericDepth == 0) {
                normalizedName.Append(character);
            }
        }

        return normalizedName.ToString();
    }

    private static string NormalizeDocumentationName(string name) {
        var genericIndex = name.IndexOf('<');
        return genericIndex < 0 ? name : name[..genericIndex];
    }

    private readonly record struct SearchQuery(string Text, string[] Terms);

    private readonly record struct SearchMatch(int Score, IReadOnlyList<string> MatchedTerms, IReadOnlyList<string> MatchedFields) {
        public static SearchMatch Empty { get; } = new(0, [], []);
    }

    private readonly record struct TermMatch(int Score, IReadOnlySet<string> Fields);
}
