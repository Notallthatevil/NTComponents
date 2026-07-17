using ModelContextProtocol.Server;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using System.ComponentModel;

namespace NTComponents.MCP.Tools;

[McpServerToolType]
public sealed class NTComponentsTools(NTComponentsCatalog _catalog) {
    [McpServerTool(Name = "list_nt_components"), Description("Lists public NT-prefixed Blazor components with summaries, render compatibility, source folder, and required parameters. Use query or folder to narrow the result.")]
    public IReadOnlyList<ComponentSummary> ListComponents(
        [Description("Optional text matched against component names and documentation.")] string? query = null,
        [Description("Optional source folder such as Buttons, Form, Grid, Dialog, or Layout.")] string? folder = null,
        [Description("Include obsolete components when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 200.")] int limit = 100) =>
        _catalog.ListComponents(query, folder, includeObsolete, limit);

    [McpServerTool(Name = "get_nt_component"), Description("Gets complete usage guidance for one NT-prefixed component, including parameters, required inputs, render-mode compatibility, methods, related enums/helpers, and obsolescence guidance.")]
    public LookupResult<ComponentDetails> GetComponent([Description("Component name or full type name, for example NTButton or NTComponents.NTButton.")] string name) =>
        _catalog.GetComponent(name) is { } component ? LookupResult<ComponentDetails>.Success(component) : LookupResult<ComponentDetails>.Missing($"Component '{name}' was not found.");

    [McpServerTool(Name = "list_nt_reference_types"), Description("Lists public enums and helper types that are part of or referenced by the NTComponents component API.")]
    public IReadOnlyList<ReferenceSummary> ListReferenceTypes(
        [Description("Optional text matched against reference type names and documentation.")] string? query = null,
        [Description("Optional kind: Enum or Helper.")] string? kind = null,
        [Description("Include obsolete reference types when true.")] bool includeObsolete = false,
        [Description("Maximum results from 1 through 200.")] int limit = 100) =>
        _catalog.ListReferences(query, kind, includeObsolete, limit);

    [McpServerTool(Name = "get_nt_reference_type"), Description("Gets values and usage details for one NTComponents enum or helper type, including components that reference it.")]
    public LookupResult<ReferenceDetails> GetReferenceType([Description("Reference type name or full name, for example NTButtonVariant or NTComponents.NTButtonVariant.")] string name) =>
        _catalog.GetReference(name) is { } reference ? LookupResult<ReferenceDetails>.Success(reference) : LookupResult<ReferenceDetails>.Missing($"Reference type '{name}' was not found.");

    [McpServerTool(Name = "search_ntcomponents"), Description("Searches component, enum, and helper documentation and returns relevance-ranked matches.")]
    public IReadOnlyList<DocumentationSearchResult> Search(
        [Description("Required search text, such as dialog, elevation, render compatibility, or a type name.")] string query,
        [Description("Maximum results from 1 through 200.")] int limit = 25) =>
        _catalog.Search(query, limit);
}
