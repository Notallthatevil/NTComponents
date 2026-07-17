using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using System.ComponentModel;

namespace NTComponents.MCP.Endpoints;

public static class DocumentationEndpoints {
    public static IEndpointRouteBuilder MapNTComponentsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/", (NTComponentsCatalog catalog) => TypedResults.Ok(new ServiceDiscovery("NTComponents.MCP", "/mcp", "/openapi/v1.json", "/health", "/api", catalog.GetOverview())))
            .WithName("DiscoverNTComponentsService")
            .WithSummary("Discover the NTComponents documentation service")
            .WithDescription("Returns the MCP, OpenAPI, health, and REST API locations together with current catalog counts.")
            .WithTags("Service")
            .Produces<ServiceDiscovery>();

        endpoints.MapGet("/health", () => TypedResults.Ok(new HealthStatus("Healthy")))
            .WithName("GetHealth")
            .WithSummary("Check service health")
            .WithDescription("Returns a healthy status when the documentation catalog service is available.")
            .WithTags("Service")
            .Produces<HealthStatus>();

        var api = endpoints.MapGroup("/api").WithTags("NTComponents Documentation");
        api.MapGet("/", (NTComponentsCatalog catalog) => TypedResults.Ok(catalog.GetOverview()))
            .WithName("GetCatalogOverview")
            .WithSummary("Get the documentation catalog overview")
            .WithDescription("Returns current component and reference-type counts plus available component folders and reference kinds.")
            .Produces<CatalogOverview>();

        api.MapGet("/components", (NTComponentsCatalog catalog,
            [Description("Optional text matched against component names and generated documentation.")] string? query,
            [Description("Optional source folder such as Buttons, Form, Grid, Dialog, or Layout.")] string? folder,
            [Description("Include obsolete components when true.")] bool includeObsolete = false,
            [Description("Maximum number of results from 1 through 200.")] int limit = 100) => TypedResults.Ok(catalog.ListComponents(query, folder, includeObsolete, limit)))
            .WithName("ListNTComponents")
            .WithSummary("List NT-prefixed components")
            .WithDescription("Lists public NT-prefixed Blazor components with generated summaries, render compatibility, source folders, and required parameters.")
            .Produces<IReadOnlyList<ComponentSummary>>();

        api.MapGet("/components/{name}", (NTComponentsCatalog catalog,
            [Description("Component name or full name, for example NTButton or NTComponents.NTButton.")] string name) =>
            catalog.GetComponent(name) is { } component ? Results.Ok(component) : Results.NotFound(new ErrorResponse($"Component '{name}' was not found.")))
            .WithName("GetNTComponent")
            .WithSummary("Get one NT-prefixed component")
            .WithDescription("Returns generated usage guidance, parameters, methods, render-mode compatibility, related enums and helpers, and obsolescence information for one component.")
            .Produces<ComponentDetails>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        api.MapGet("/references", (NTComponentsCatalog catalog,
            [Description("Optional text matched against reference-type names and generated documentation.")] string? query,
            [Description("Optional reference kind: Enum or Helper.")] string? kind,
            [Description("Include obsolete reference types when true.")] bool includeObsolete = false,
            [Description("Maximum number of results from 1 through 200.")] int limit = 100) => TypedResults.Ok(catalog.ListReferences(query, kind, includeObsolete, limit)))
            .WithName("ListNTReferenceTypes")
            .WithSummary("List NTComponents enums and helpers")
            .WithDescription("Lists public enums and helper types that are part of or referenced by the NTComponents component API.")
            .Produces<IReadOnlyList<ReferenceSummary>>();

        api.MapGet("/references/{name}", (NTComponentsCatalog catalog,
            [Description("Reference type name or full name, for example NTButtonVariant or NTComponents.NTButtonVariant.")] string name) =>
            catalog.GetReference(name) is { } reference ? Results.Ok(reference) : Results.NotFound(new ErrorResponse($"Reference type '{name}' was not found.")))
            .WithName("GetNTReferenceType")
            .WithSummary("Get one NTComponents enum or helper")
            .WithDescription("Returns generated documentation, enum values or public members, obsolescence information, and the components that use one reference type.")
            .Produces<ReferenceDetails>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        api.MapGet("/search", (NTComponentsCatalog catalog,
            [Description("Required text matched against component, enum, and helper documentation.")] string query,
            [Description("Maximum number of results from 1 through 200.")] int limit = 25) => TypedResults.Ok(catalog.Search(query, limit)))
            .WithName("SearchNTComponents")
            .WithSummary("Search NTComponents documentation")
            .WithDescription("Returns relevance-ranked component, enum, and helper matches from the generated documentation catalog.")
            .Produces<IReadOnlyList<DocumentationSearchResult>>();

        return endpoints;
    }
}
