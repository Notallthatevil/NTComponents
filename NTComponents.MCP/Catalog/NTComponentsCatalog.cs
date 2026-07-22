using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using NTComponents.GeneratedDocumentation;
using NTComponents.MCP.Contracts;

namespace NTComponents.MCP.Catalog;

public sealed class NTComponentsCatalog {
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

    public CatalogOverview GetOverview() => new(
        _components.Count,
        _references.Count,
        _components.Select(component => component.SourceFolder).Where(folder => !string.IsNullOrWhiteSpace(folder)).Distinct(StringComparer.Ordinal).OrderBy(folder => folder, StringComparer.Ordinal).ToArray(),
        _references.Select(GetReferenceKind).Distinct(StringComparer.Ordinal).OrderBy(kind => kind, StringComparer.Ordinal).ToArray());

    public IReadOnlyList<ComponentSummary> ListComponents(string? query = null, string? folder = null, bool includeObsolete = false, int limit = 100) {
        CatalogInputValidator.ValidateLimit(limit);
        CatalogInputValidator.ValidateOptionalQuery(query);
        SearchQuery? searchQuery = string.IsNullOrWhiteSpace(query) ? null : CreateSearchQuery(query);
        return _components
            .Where(component => includeObsolete || !component.IsObsolete)
            .Where(component => string.IsNullOrWhiteSpace(folder) || string.Equals(component.SourceFolder, folder, StringComparison.OrdinalIgnoreCase))
            .Where(component => Matches(component, searchQuery, isComponent: true))
            .Take(limit)
            .Select(ToComponentSummary)
            .ToArray();
    }

    public ComponentDetails? GetComponent(string name, bool includeRelatedEnumValues = false) {
        CatalogInputValidator.ValidateRequiredText(name, nameof(name));
        return TryFind(_componentsByName, name, out var component) ? ToComponentDetails(component, includeRelatedEnumValues) : null;
    }

    public IReadOnlyList<ReferenceSummary> ListReferences(string? query = null, string? kind = null, bool includeObsolete = false, int limit = 100) {
        CatalogInputValidator.ValidateLimit(limit);
        CatalogInputValidator.ValidateOptionalQuery(query);
        CatalogInputValidator.ValidateReferenceKind(kind);
        SearchQuery? searchQuery = string.IsNullOrWhiteSpace(query) ? null : CreateSearchQuery(query);
        return _references
            .Where(reference => includeObsolete || !reference.IsObsolete)
            .Where(reference => kind is null || string.Equals(GetReferenceKind(reference), kind, StringComparison.OrdinalIgnoreCase))
            .Where(reference => Matches(reference, searchQuery, isComponent: false))
            .Take(limit)
            .Select(ToReferenceSummary)
            .ToArray();
    }

    public ReferenceDetails? GetReference(string name) {
        CatalogInputValidator.ValidateRequiredText(name, nameof(name));
        return TryFind(_referencesByName, name, out var reference) ? ToReferenceDetails(reference) : null;
    }

    public IReadOnlyList<DocumentationSearchResult> Search(string query, int limit = 25) {
        CatalogInputValidator.ValidateRequiredQuery(query);
        CatalogInputValidator.ValidateLimit(limit);
        var searchQuery = CreateSearchQuery(query);
        var results = new List<DocumentationSearchResult>();
        foreach (var component in _components) {
            var score = CalculateScore(component, searchQuery, isComponent: true);
            if (score > 0) {
                results.Add(new(component.Name, component.FullName, "Component", component.Summary, component.SourceFolder, score));
            }
        }

        foreach (var reference in _references) {
            var score = CalculateScore(reference, searchQuery, isComponent: false);
            if (score > 0) {
                results.Add(new(reference.Name, reference.FullName, GetReferenceKind(reference), reference.Summary, reference.SourceFolder, score));
            }
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Name, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
    }

    private ComponentDetails ToComponentDetails(TypeDocumentation component, bool includeRelatedEnumValues) {
        var relatedReferences = _references.Where(reference => UsesType(component, reference)).ToArray();
        var relatedTypes = relatedReferences.Select(ToReferenceSummary).ToArray();
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
            component.Parameters.Where(parameter => IsPublicOrProtected(parameter.Accessibility)).Select(ToParameterDetails).ToArray(),
            component.Methods.Where(IsConsumerMethod).Select(ToMemberDetails).ToArray(),
            relatedTypes,
            guidelines,
            GetRazorUsage(component),
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
        reference.Properties.Where(property => !property.IsFromBaseType && IsPublicOrProtected(property.Accessibility)).Select(ToParameterDetails).ToArray(),
        reference.Methods.Where(method => !method.IsFromBaseType && IsPublicOrProtected(method.Accessibility)).Select(ToMemberDetails).ToArray(),
        GetUsingComponents(reference));

    private ComponentSummary ToComponentSummary(TypeDocumentation component) => new(
        component.Name,
        component.FullName,
        component.SourceFolder,
        component.Summary,
        component.RenderCompatibility,
        component.IsObsolete,
        component.Parameters.Where(parameter => parameter.IsEditorRequired).Select(parameter => parameter.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

    private ReferenceSummary ToReferenceSummary(TypeDocumentation reference) => new(reference.Name, reference.FullName, GetReferenceKind(reference), reference.Summary, reference.IsObsolete, GetUsingComponents(reference));

    private IReadOnlyList<string> GetUsingComponents(TypeDocumentation reference) => _components.Where(component => UsesType(component, reference)).Select(component => component.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private RelatedEnumDetails ToRelatedEnumDetails(TypeDocumentation reference) {
        var values = reference.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility)).ToArray();
        return new(reference.Name, reference.FullName, values.Take(MaximumInlineEnumValues).Select(ToFieldDetails).ToArray(), values.Length, values.Length > MaximumInlineEnumValues);
    }

    private static ParameterDetails ToParameterDetails(PropertyDocumentation parameter) => new(
        parameter.Name,
        parameter.TypeDisplayName,
        parameter.Summary,
        parameter.IsEditorRequired,
        parameter.IsCascadingParameter,
        parameter.IsFromBaseType,
        parameter.IsObsolete,
        parameter.Accessibility,
        string.IsNullOrEmpty(parameter.DefaultValueExpression) ? null : parameter.DefaultValueExpression);

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

    private static int CalculateScore(TypeDocumentation type, SearchQuery query, bool isComponent) {
        if (string.Equals(type.Name, query.Text, StringComparison.OrdinalIgnoreCase) || string.Equals(type.FullName, query.Text, StringComparison.OrdinalIgnoreCase)) {
            return 1000;
        }

        if (query.Terms.Length == 0) {
            return 0;
        }

        var score = 0;
        var matchedTerms = 0;
        foreach (var term in query.Terms) {
            var termScore = CalculateTermScore(type, term, isComponent);
            if (termScore == 0) {
                continue;
            }

            score += termScore;
            matchedTerms++;
        }

        return matchedTerms == query.Terms.Length ? score + 100 : score;
    }

    private static int CalculateTermScore(TypeDocumentation type, string term, bool isComponent) {
        var score = Math.Max(ScoreText(type.Name, term, 100, 70), ScoreText(type.FullName, term, 90, 60));
        score = Math.Max(score, ScoreText(type.SourceFolder, term, 35, 20));
        score = Math.Max(score, ContainsTerm(type.Summary, term) ? 50 : 0);
        score = Math.Max(score, ContainsTerm(type.Remarks, term) ? 15 : 0);

        if (!string.Equals(type.RenderCompatibility, "Unknown", StringComparison.Ordinal)) {
            score = Math.Max(score, ContainsTerm(nameof(type.RenderCompatibility), term) ? 50 : 0);
            score = Math.Max(score, ScoreText(type.RenderCompatibility, term, 45, 40));
            score = Math.Max(score, ContainsTerm(type.CompatibilitySummary, term) || ContainsTerm(type.CompatibilityDetails, term) ? 25 : 0);
        }

        foreach (var property in type.Properties.Where(property => IsPublicOrProtected(property.Accessibility))) {
            score = Math.Max(score, ScoreText(property.Name, term, 45, 35));
            score = Math.Max(score, ContainsTerm(property.Signature, term) || ContainsTerm(property.TypeDisplayName, term) || ContainsTerm(property.TypeFullName, term) || ContainsTerm(property.Summary, term) ? 20 : 0);
        }

        foreach (var method in type.Methods) {
            if (isComponent ? !IsConsumerMethod(method) : method.IsFromBaseType || !IsPublicOrProtected(method.Accessibility)) {
                continue;
            }

            score = Math.Max(score, ScoreText(method.Name, term, 45, 35));
            score = Math.Max(score, ContainsTerm(method.Signature, term) || ContainsTerm(method.Summary, term) ? 20 : 0);
        }

        foreach (var field in type.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility))) {
            score = Math.Max(score, ScoreText(field.Name, term, 45, 35));
            score = Math.Max(score, ContainsTerm(field.Signature, term) || ContainsTerm(field.TypeDisplayName, term) || ContainsTerm(field.TypeFullName, term) || ContainsTerm(field.ConstantValue, term) || ContainsTerm(field.Summary, term) ? 20 : 0);
        }

        return score;
    }

    private static SearchQuery CreateSearchQuery(string query) {
        var normalizedQuery = query.Trim();
        var terms = normalizedQuery
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '<', '>'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        CatalogInputValidator.ValidateQueryTermCount(terms.Length);
        return new(normalizedQuery, terms);
    }

    private static int ScoreText(string text, string term, int exactScore, int containsScore) =>
        string.Equals(text, term, StringComparison.OrdinalIgnoreCase) ? exactScore : ContainsTerm(text, term) ? containsScore : 0;

    private static bool ContainsTerm(string text, string term) => text.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool Matches(TypeDocumentation type, SearchQuery? query, bool isComponent) => query is null || CalculateScore(type, query.Value, isComponent) > 0;

    private static bool UsesType(TypeDocumentation component, TypeDocumentation reference) {
        var referenceFullName = NormalizeDocumentationFullName(reference.FullName);
        var referenceName = NormalizeDocumentationName(reference.Name);
        return component.Properties.Any(property => ContainsType(property.TypeFullName, property.TypeDisplayName, referenceFullName, referenceName))
            || component.Methods.Any(method => method.Signature.Contains(referenceFullName, StringComparison.Ordinal) || method.Signature.Contains(referenceName, StringComparison.Ordinal));
    }

    private static bool ContainsType(string fullTypeName, string displayTypeName, string referenceFullName, string referenceName) =>
        fullTypeName.Contains(referenceFullName, StringComparison.Ordinal) || displayTypeName.Contains(referenceName, StringComparison.Ordinal);

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
}
