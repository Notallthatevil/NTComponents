using ModelContextProtocol;
using ModelContextProtocol.Server;
using NTComponents.MCP.Catalog;
using System.ComponentModel;
using System.Text.Json;

namespace NTComponents.MCP.Resources;

[McpServerResourceType]
public sealed class NTComponentsResources(NTComponentsCatalog _catalog) {
    private const int ReferenceMemberLimit = 50;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerResource(UriTemplate = "ntcomponents://catalog", Name = "ntcomponents_catalog", Title = "NTComponents catalog overview", MimeType = "application/json")]
    [Description("Current NTComponents MCP and component-library versions, documentation location, catalog counts, and supported filters.")]
    public string GetCatalogOverview() => Serialize(_catalog.GetOverview());

    [McpServerResource(UriTemplate = "ntcomponents://components/{name}", Name = "ntcomponents_component", Title = "NTComponents component documentation", MimeType = "application/json")]
    [Description("Structured documentation for one NT-prefixed component, addressed by simple or full type name.")]
    public string GetComponent(string name) => Serialize(_catalog.GetComponent(name) ?? throw new McpException($"Component '{name}' was not found."));

    [McpServerResource(UriTemplate = "ntcomponents://references/{name}", Name = "ntcomponents_reference", Title = "NTComponents reference-type documentation", MimeType = "application/json")]
    [Description("Structured documentation and the first 50 members for one NTComponents enum or helper. Use get_nt_reference_type with query, or pass nextOffset as offset, for additional members.")]
    public string GetReference(string name) => Serialize(McpReferenceProjector.Project(_catalog.GetReference(name) ?? throw new McpException($"Reference type '{name}' was not found."), null, ReferenceMemberLimit, 0));

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
}
