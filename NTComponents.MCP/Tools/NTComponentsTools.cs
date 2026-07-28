using ModelContextProtocol;
using ModelContextProtocol.Server;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.MCP.Tools;

[McpServerToolType]
public sealed class NTComponentsTools(NTComponentsCatalog _catalog) {
    private const int DefaultListLimit = 10;
    private const int DefaultSearchLimit = 5;

    [McpServerTool(Name = "get_nt_catalog_overview", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets catalog versions, build revision, documentation URL, counts, folders, kinds, and supported reference scopes. Call this first when catalog freshness or capabilities matter.")]
    public CatalogOverview GetCatalogOverview() => _catalog.GetOverview();

    [McpServerTool(Name = "list_nt_components", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Lists public NT-prefixed Blazor components as a compact paged result. Use query or folder to narrow results, then call get_nt_component for usage guidance.")]
    public McpPage<McpComponentSummary> ListComponents(
        [Description("Optional text matched against component names and documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Optional source folder such as Buttons, Form, Grid, Dialog, or Layout.")] string? folder = null,
        [Description("Include obsolete components when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 50."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumMcpLimit)] int limit = DefaultListLimit,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => {
            CatalogInputValidator.ValidateMcpLimit(limit);
            return MapPage(_catalog.ListComponentPage(query, folder, includeObsolete, limit, offset), ToMcpSummary);
        });

    [McpServerTool(Name = "get_nt_component", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets concise usage guidance for one NT-prefixed component. Call get_nt_component_members for parameters or methods, get_nt_reference_type for related types, or read resourceUri for exhaustive documentation.")]
    public LookupResult<ComponentUsageSummary> GetComponent([Description("Component name or full type name, for example NTButton or NTComponents.NTButton."), Required, MinLength(1)] string name) =>
        Invoke(() => _catalog.GetComponent(name) is { } component ? LookupResult<ComponentUsageSummary>.Success(ToUsageSummary(component)) : LookupResult<ComponentUsageSummary>.Missing($"Component '{name}' was not found."));

    [McpServerTool(Name = "get_nt_component_members", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets a compact paged list of parameters and consumer-callable methods for one component. Use query, kind, and includeInherited to request only the API details needed.")]
    public LookupResult<McpPage<ComponentMemberSummary>> GetComponentMembers(
        [Description("Component name or full type name, for example NTButton or NTComponents.NTButton."), Required, MinLength(1)] string name,
        [Description("Optional text matched against member names, declarations, summaries, and parameter categories."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Optional member kind: Parameter or Method."), AllowedValues(CatalogInputValidator.ParameterMemberKind, CatalogInputValidator.MethodMemberKind)] string? kind = null,
        [Description("Include inherited parameters when true.")] bool includeInherited = false,
        [Description("Maximum results from 1 through 50."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumMcpLimit)] int limit = DefaultListLimit,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => GetComponentMemberPage(name, query, kind, includeInherited, limit, offset));

    [McpServerTool(Name = "list_nt_reference_types", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Lists public enums and helper types as a compact paged result. Use scope to narrow results, then call get_nt_reference_type only for specific values or members.")]
    public McpPage<McpReferenceSummary> ListReferenceTypes(
        [Description("Optional text matched against reference type names and documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Optional kind: Enum or Helper."), AllowedValues(CatalogInputValidator.EnumReferenceKind, CatalogInputValidator.HelperReferenceKind)] string? kind = null,
        [Description("Optional scope: ComponentApi for types used by components, or LibraryApi for other public library types."), AllowedValues(CatalogInputValidator.ComponentApiReferenceScope, CatalogInputValidator.LibraryApiReferenceScope)] string? scope = null,
        [Description("Include obsolete reference types when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 50."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumMcpLimit)] int limit = DefaultListLimit,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => {
            CatalogInputValidator.ValidateMcpLimit(limit);
            return MapPage(_catalog.ListReferencePage(query, kind, scope, includeObsolete, limit, offset), ToMcpSummary);
        });

    [McpServerTool(Name = "get_nt_reference_type", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets concise usage details and a paged member list for one NTComponents enum or helper type. Use query to narrow members and pass nextOffset as offset for another page.")]
    public LookupResult<ReferenceUsageSummary> GetReferenceType(
        [Description("Reference type name or full name, for example NTButtonVariant or NTComponents.NTButtonVariant."), Required, MinLength(1)] string name,
        [Description("Optional text matched against member names, declarations, and summaries."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Maximum members from 1 through 50."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumMcpLimit)] int limit = DefaultListLimit,
        [Description("Zero-based member offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => {
            CatalogInputValidator.ValidateMcpLimit(limit);
            CatalogInputValidator.ValidateOffset(offset);
            CatalogInputValidator.ValidateOptionalQuery(query);
            return _catalog.GetReference(name) is { } reference
                ? LookupResult<ReferenceUsageSummary>.Success(McpReferenceProjector.Project(reference, query, limit, offset))
                : LookupResult<ReferenceUsageSummary>.Missing($"Reference type '{name}' was not found.");
        });

    [McpServerTool(Name = "search_ntcomponents", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Searches component, enum, and helper documentation and returns compact paged relevance-ranked matches with documentation links and typo suggestions.")]
    public McpDocumentationSearchPage Search(
        [Description("Required search text, such as dialog, elevation, render compatibility, or a type name."), Required, MinLength(1), MaxLength(CatalogInputValidator.MaximumQueryLength)] string query,
        [Description("Maximum results from 1 through 50."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumMcpLimit)] int limit = DefaultSearchLimit,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => {
            CatalogInputValidator.ValidateMcpLimit(limit);
            var page = _catalog.SearchPage(query, limit, offset);
            return new McpDocumentationSearchPage(page.Items.Select(ToMcpSummary).ToArray(), page.TotalCount, page.NextOffset, NullIfEmpty(page.DidYouMean));
        });

    private LookupResult<McpPage<ComponentMemberSummary>> GetComponentMemberPage(string name, string? query, string? kind, bool includeInherited, int limit, int offset) {
        CatalogInputValidator.ValidateMcpLimit(limit);
        CatalogInputValidator.ValidateOffset(offset);
        CatalogInputValidator.ValidateOptionalQuery(query);
        CatalogInputValidator.ValidateMemberKind(kind);
        if (_catalog.GetComponent(name) is not { } component) {
            return LookupResult<McpPage<ComponentMemberSummary>>.Missing($"Component '{name}' was not found.");
        }

        var members = component.Parameters
            .Where(parameter => includeInherited || !parameter.IsInherited)
            .Select(parameter => new ComponentMemberSummary(parameter.Name, CatalogInputValidator.ParameterMemberKind, parameter.Type, parameter.Summary, TrueOrNull(parameter.IsRequired), TrueOrNull(parameter.IsInherited), TrueOrNull(parameter.IsObsolete), NullIfEmpty(parameter.DefaultValueExpression), NullIfEmpty(parameter.Category)))
            .Concat(component.Methods.Select(method => new ComponentMemberSummary(method.Name, CatalogInputValidator.MethodMemberKind, method.Signature, method.Summary, null, TrueOrNull(method.IsInherited), TrueOrNull(method.IsObsolete), null, null)))
            .Where(member => kind is null || string.Equals(member.Kind, kind, StringComparison.OrdinalIgnoreCase))
            .Where(member => string.IsNullOrWhiteSpace(query) || MatchesMember(member, query))
            .OrderBy(member => member.Kind, StringComparer.Ordinal)
            .ThenBy(member => member.Category, StringComparer.Ordinal)
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .ToArray();
        var items = members.Skip(offset).Take(limit).ToArray();
        var nextOffset = (long)offset + items.Length;
        return LookupResult<McpPage<ComponentMemberSummary>>.Success(new(items, members.Length, nextOffset < members.Length ? (int)nextOffset : null));
    }

    private static bool MatchesMember(ComponentMemberSummary member, string query) =>
        member.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || member.Declaration.Contains(query, StringComparison.OrdinalIgnoreCase)
        || member.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (member.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private static ComponentUsageSummary ToUsageSummary(ComponentDetails component) => new(
        component.Name,
        component.Summary,
        component.RenderCompatibility,
        TrueOrNull(component.IsObsolete),
        NullIfEmpty(component.ObsoleteMessage),
        NullIfEmpty(component.Parameters.Where(parameter => parameter.IsRequired).Select(parameter => new RequiredParameterSummary(parameter.Name, parameter.Type, parameter.Summary)).ToArray()),
        NullIfEmpty(component.UsageGuidelines),
        component.RazorUsage,
        NullIfEmpty(component.RelatedTypes.Select(reference => reference.Name).ToArray()),
        NullIfEmpty(component.RelatedComponents.Select(relatedComponent => relatedComponent.Name).ToArray()),
        component.Parameters.Count,
        component.Methods.Count,
        component.DocumentationUrl,
        $"ntcomponents://components/{component.Name}");

    private static McpComponentSummary ToMcpSummary(ComponentSummary component) => new(component.Name, component.Folder, component.Summary, component.RenderCompatibility, TrueOrNull(component.IsObsolete), NullIfEmpty(component.RequiredParameters), component.DocumentationUrl);

    private static McpReferenceSummary ToMcpSummary(ReferenceSummary reference) => new(reference.Name, reference.Kind, reference.Summary, TrueOrNull(reference.IsObsolete), reference.Scope, reference.DocumentationUrl);

    private static McpDocumentationSearchResult ToMcpSummary(DocumentationSearchResult result) => new(result.Name, result.Category, result.Summary, result.DocumentationUrl);

    private static McpPage<TOutput> MapPage<TInput, TOutput>(CatalogPage<TInput> page, Func<TInput, TOutput> map) => new(page.Items.Select(map).ToArray(), page.TotalCount, page.NextOffset);

    private static bool? TrueOrNull(bool value) => value ? true : null;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static IReadOnlyList<T>? NullIfEmpty<T>(IReadOnlyList<T> values) => values.Count == 0 ? null : values;

    private static T Invoke<T>(Func<T> operation) {
        try {
            return operation();
        }
        catch (CatalogValidationException exception) {
            throw new McpException(exception.Message, exception);
        }
    }
}
