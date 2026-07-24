using ModelContextProtocol;
using ModelContextProtocol.Server;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NTComponents.MCP.Tools;

[McpServerToolType]
public sealed class NTComponentsTools(NTComponentsCatalog _catalog) {
    [McpServerTool(Name = "get_nt_catalog_overview", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets catalog versions, build revision, documentation URL, counts, folders, kinds, and supported reference scopes. Call this first when catalog freshness or capabilities matter.")]
    public CatalogOverview GetCatalogOverview() => _catalog.GetOverview();

    [McpServerTool(Name = "list_nt_components", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Lists public NT-prefixed Blazor components as a paged result with summaries, documentation links, render compatibility, source folder, and required parameters. Use query or folder to narrow the result.")]
    public CatalogPage<ComponentSummary> ListComponents(
        [Description("Optional text matched against component names and documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Optional source folder such as Buttons, Form, Grid, Dialog, or Layout.")] string? folder = null,
        [Description("Include obsolete components when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 100,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => _catalog.ListComponentPage(query, folder, includeObsolete, limit, offset));

    [McpServerTool(Name = "get_nt_component", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets complete usage guidance for one NT-prefixed component, including organized parameters, composition-aware examples, related components, render-mode compatibility, methods, documentation link, and related enums/helpers.")]
    public LookupResult<ComponentDetails> GetComponent(
        [Description("Component name or full type name, for example NTButton or NTComponents.NTButton."), Required, MinLength(1)] string name,
        [Description("Inline up to 20 values for each related enum when true. Use get_nt_reference_type for complete large enums.")] bool includeRelatedEnumValues = false) =>
        Invoke(() => _catalog.GetComponent(name, includeRelatedEnumValues) is { } component ? LookupResult<ComponentDetails>.Success(component) : LookupResult<ComponentDetails>.Missing($"Component '{name}' was not found."));

    [McpServerTool(Name = "list_nt_reference_types", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Lists public enums and helper types as a paged result. Use scope to distinguish types used by component APIs from the broader public library API.")]
    public CatalogPage<ReferenceSummary> ListReferenceTypes(
        [Description("Optional text matched against reference type names and documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query = null,
        [Description("Optional kind: Enum or Helper."), AllowedValues(CatalogInputValidator.EnumReferenceKind, CatalogInputValidator.HelperReferenceKind)] string? kind = null,
        [Description("Optional scope: ComponentApi for types used by components, or LibraryApi for other public library types."), AllowedValues(CatalogInputValidator.ComponentApiReferenceScope, CatalogInputValidator.LibraryApiReferenceScope)] string? scope = null,
        [Description("Include obsolete reference types when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 100,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => _catalog.ListReferencePage(query, kind, scope, includeObsolete, limit, offset));

    [McpServerTool(Name = "get_nt_reference_type", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Gets values and usage details for one NTComponents enum or helper type, including its API scope, documentation link, and components that reference it.")]
    public LookupResult<ReferenceDetails> GetReferenceType([Description("Reference type name or full name, for example NTButtonVariant or NTComponents.NTButtonVariant."), Required, MinLength(1)] string name) =>
        Invoke(() => _catalog.GetReference(name) is { } reference ? LookupResult<ReferenceDetails>.Success(reference) : LookupResult<ReferenceDetails>.Missing($"Reference type '{name}' was not found."));

    [McpServerTool(Name = "search_ntcomponents", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false, UseStructuredContent = true), Description("Searches component, enum, and helper documentation and returns paged relevance-ranked matches with matched terms, matched fields, documentation links, and typo suggestions.")]
    public DocumentationSearchPage Search(
        [Description("Required search text, such as dialog, elevation, render compatibility, or a type name."), Required, MinLength(1), MaxLength(CatalogInputValidator.MaximumQueryLength)] string query,
        [Description("Maximum results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 25,
        [Description("Zero-based result offset. Use nextOffset from the prior page."), Range(0, int.MaxValue)] int offset = 0) =>
        Invoke(() => _catalog.SearchPage(query, limit, offset));

    private static T Invoke<T>(Func<T> operation) {
        try {
            return operation();
        }
        catch (CatalogValidationException exception) {
            throw new McpException(exception.Message, exception);
        }
    }
}
