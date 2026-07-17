using Microsoft.AspNetCore.Components;
using NTComponents.GeneratedDocumentation;
using NTComponents.MCP.Contracts;

namespace NTComponents.MCP.Catalog;

public sealed class NTComponentsCatalog {
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

    public IReadOnlyList<ComponentSummary> ListComponents(string? query = null, string? folder = null, bool includeObsolete = false, int limit = 100) =>
        _components
            .Where(component => includeObsolete || !component.IsObsolete)
            .Where(component => string.IsNullOrWhiteSpace(folder) || string.Equals(component.SourceFolder, folder, StringComparison.OrdinalIgnoreCase))
            .Where(component => Matches(component, query))
            .Take(NormalizeLimit(limit))
            .Select(ToComponentSummary)
            .ToArray();

    public ComponentDetails? GetComponent(string name) => TryFind(_componentsByName, name, out var component) ? ToComponentDetails(component) : null;

    public IReadOnlyList<ReferenceSummary> ListReferences(string? query = null, string? kind = null, bool includeObsolete = false, int limit = 100) =>
        _references
            .Where(reference => includeObsolete || !reference.IsObsolete)
            .Where(reference => string.IsNullOrWhiteSpace(kind) || string.Equals(GetReferenceKind(reference), kind, StringComparison.OrdinalIgnoreCase))
            .Where(reference => Matches(reference, query))
            .Take(NormalizeLimit(limit))
            .Select(ToReferenceSummary)
            .ToArray();

    public ReferenceDetails? GetReference(string name) => TryFind(_referencesByName, name, out var reference) ? ToReferenceDetails(reference) : null;

    public IReadOnlyList<DocumentationSearchResult> Search(string query, int limit = 25) {
        if (string.IsNullOrWhiteSpace(query)) {
            return [];
        }

        return _components.Select(component => ToSearchResult(component, "Component", query))
            .Concat(_references.Select(reference => ToSearchResult(reference, GetReferenceKind(reference), query)))
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Name, StringComparer.Ordinal)
            .Take(NormalizeLimit(limit))
            .ToArray();
    }

    private ComponentDetails ToComponentDetails(TypeDocumentation component) {
        var relatedTypes = _references.Where(reference => UsesType(component, reference)).Select(ToReferenceSummary).ToArray();
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
            component.Methods.Where(method => !method.IsFromBaseType && IsPublicOrProtected(method.Accessibility)).Select(ToMemberDetails).ToArray(),
            relatedTypes,
            guidelines);
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
        reference.Fields.Where(field => !field.IsFromBaseType && IsPublicOrProtected(field.Accessibility)).Select(field => new FieldDetails(field.Name, field.TypeDisplayName, field.ConstantValue, field.Summary, field.IsObsolete)).ToArray(),
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

    private static ParameterDetails ToParameterDetails(PropertyDocumentation parameter) => new(parameter.Name, parameter.TypeDisplayName, parameter.Summary, parameter.IsEditorRequired, parameter.IsCascadingParameter, parameter.IsFromBaseType, parameter.IsObsolete);

    private static MemberDetails ToMemberDetails(MethodDocumentation method) => new(method.Name, method.Signature, method.Summary, method.IsFromBaseType, method.IsObsolete);

    private static DocumentationSearchResult ToSearchResult(TypeDocumentation type, string category, string query) => new(type.Name, type.FullName, category, type.Summary, type.SourceFolder, CalculateScore(type, query));

    private static int CalculateScore(TypeDocumentation type, string query) {
        var score = 0;
        if (string.Equals(type.Name, query, StringComparison.OrdinalIgnoreCase) || string.Equals(type.FullName, query, StringComparison.OrdinalIgnoreCase)) {
            score += 100;
        }
        else if (type.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) {
            score += 50;
        }
        if (type.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)) {
            score += 20;
        }
        if (type.Remarks.Contains(query, StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }
        if (type.Properties.Any(property => property.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || property.Summary.Contains(query, StringComparison.OrdinalIgnoreCase))) {
            score += 5;
        }
        return score;
    }

    private static bool Matches(TypeDocumentation type, string? query) => string.IsNullOrWhiteSpace(query) || CalculateScore(type, query) > 0;

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

    private static int NormalizeLimit(int limit) => Math.Clamp(limit, 1, 200);

    private static bool IsPublicOrProtected(string accessibility) => accessibility is "Public" or "Protected" or "ProtectedOrInternal";

    private static string NormalizeRuntimeFullName(Type type) {
        var fullName = type.FullName ?? type.Name;
        var genericIndex = fullName.IndexOf('`');
        return genericIndex < 0 ? fullName : fullName[..genericIndex];
    }

    private static string NormalizeDocumentationFullName(string fullName) {
        var genericIndex = fullName.IndexOf('<');
        return genericIndex < 0 ? fullName : fullName[..genericIndex];
    }

    private static string NormalizeDocumentationName(string name) {
        var genericIndex = name.IndexOf('<');
        return genericIndex < 0 ? name : name[..genericIndex];
    }
}
