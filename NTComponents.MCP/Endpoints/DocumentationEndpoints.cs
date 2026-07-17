using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Endpoints;

public static class DocumentationEndpoints {
    public static IEndpointRouteBuilder MapNTComponentsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/", (NTComponentsCatalog catalog) => Results.Ok(new {
            name = "NTComponents.MCP",
            mcp = "/mcp",
            health = "/health",
            api = "/api",
            catalog = catalog.GetOverview()
        }));
        endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

        var api = endpoints.MapGroup("/api");
        api.MapGet("/", (NTComponentsCatalog catalog) => Results.Ok(catalog.GetOverview()));
        api.MapGet("/components", (NTComponentsCatalog catalog, string? query, string? folder, bool includeObsolete = false, int limit = 100) => Results.Ok(catalog.ListComponents(query, folder, includeObsolete, limit)));
        api.MapGet("/components/{name}", (NTComponentsCatalog catalog, string name) => catalog.GetComponent(name) is { } component ? Results.Ok(component) : Results.NotFound(new { error = $"Component '{name}' was not found." }));
        api.MapGet("/references", (NTComponentsCatalog catalog, string? query, string? kind, bool includeObsolete = false, int limit = 100) => Results.Ok(catalog.ListReferences(query, kind, includeObsolete, limit)));
        api.MapGet("/references/{name}", (NTComponentsCatalog catalog, string name) => catalog.GetReference(name) is { } reference ? Results.Ok(reference) : Results.NotFound(new { error = $"Reference type '{name}' was not found." }));
        api.MapGet("/search", (NTComponentsCatalog catalog, string query, int limit = 25) => Results.Ok(catalog.Search(query, limit)));

        return endpoints;
    }
}
