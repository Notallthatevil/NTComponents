using Microsoft.OpenApi;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace NTComponents.MCP.Endpoints;

public static class DocumentationEndpoints {
    public static IEndpointRouteBuilder MapNTComponentsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/", (NTComponentsCatalog catalog) => TypedResults.Ok(new ServiceDiscovery("NTComponents.MCP", "/mcp", "/openapi/v1.json", "/health", "/api", catalog.GetOverview())))
            .WithName("DiscoverNTComponentsService")
            .WithSummary("Discover the NTComponents documentation service")
            .WithDescription("Returns the MCP, OpenAPI, health, and REST API locations together with current catalog counts.")
            .WithTags("Service")
            .Produces<ServiceDiscovery>();

        endpoints.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Check service health")
            .WithDescription("Returns a healthy status only when the documentation catalog can be resolved and contains components and reference types.")
            .WithTags("Service")
            .Produces<HealthStatus>()
            .Produces<HealthStatus>(StatusCodes.Status503ServiceUnavailable);

        var api = endpoints.MapGroup("/api")
            .WithTags("NTComponents Documentation")
            .AddEndpointFilter(async (context, next) => {
                try {
                    return await next(context);
                }
                catch (CatalogValidationException exception) {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.ParameterName] = [exception.Message] });
                }
            });
        api.MapGet("/", (NTComponentsCatalog catalog) => TypedResults.Ok(catalog.GetOverview()))
            .WithName("GetCatalogOverview")
            .WithSummary("Get the documentation catalog overview")
            .WithDescription("Returns current component and reference-type counts plus available component folders and reference kinds.")
            .Produces<CatalogOverview>();

        api.MapGet("/components", (NTComponentsCatalog catalog,
            [Description("Optional text matched against component names and generated documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query,
            [Description("Optional source folder such as Buttons, Form, Grid, Dialog, or Layout.")] string? folder,
            [Description("Include obsolete components when true.")] bool includeObsolete = false,
            [Description("Maximum number of results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 100,
            [Description("Zero-based result offset."), Range(0, int.MaxValue)] int offset = 0) => TypedResults.Ok(catalog.ListComponentPage(query, folder, includeObsolete, limit, offset)))
            .WithName("ListNTComponents")
            .WithSummary("List NT-prefixed components")
            .WithDescription("Lists public NT-prefixed Blazor components with generated summaries, render compatibility, source folders, and required parameters.")
            .Produces<CatalogPage<ComponentSummary>>()
            .ProducesValidationProblem();

        api.MapGet("/components/{name}", (NTComponentsCatalog catalog,
            [Description("Component name or full name, for example NTButton or NTComponents.NTButton."), Required, MinLength(1)] string name,
            [Description("Inline up to 20 values for each related enum when true.")] bool includeRelatedEnumValues = false) =>
            catalog.GetComponent(name, includeRelatedEnumValues) is { } component ? Results.Ok(component) : Results.NotFound(new ErrorResponse($"Component '{name}' was not found.")))
            .WithName("GetNTComponent")
            .WithSummary("Get one NT-prefixed component")
            .WithDescription("Returns generated usage guidance, parameters, methods, render-mode compatibility, related enums and helpers, and obsolescence information for one component.")
            .Produces<ComponentDetails>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        api.MapGet("/references", (NTComponentsCatalog catalog,
            [Description("Optional text matched against reference-type names and generated documentation."), MaxLength(CatalogInputValidator.MaximumQueryLength)] string? query,
            [Description("Optional reference kind: Enum or Helper."), AllowedValues(CatalogInputValidator.EnumReferenceKind, CatalogInputValidator.HelperReferenceKind)] string? kind,
            [Description("Optional API scope: ComponentApi or LibraryApi."), AllowedValues(CatalogInputValidator.ComponentApiReferenceScope, CatalogInputValidator.LibraryApiReferenceScope)] string? scope,
            [Description("Include obsolete reference types when true.")] bool includeObsolete = false,
            [Description("Maximum number of results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 100,
            [Description("Zero-based result offset."), Range(0, int.MaxValue)] int offset = 0) => TypedResults.Ok(catalog.ListReferencePage(query, kind, scope, includeObsolete, limit, offset)))
            .WithName("ListNTReferenceTypes")
            .WithSummary("List NTComponents enums and helpers")
            .WithDescription("Lists public enums and helper types that are part of or referenced by the NTComponents component API.")
            .Produces<CatalogPage<ReferenceSummary>>()
            .ProducesValidationProblem()
            .AddOpenApiOperationTransformer((operation, _, _) => {
                var kindSchema = operation.Parameters?.SingleOrDefault(parameter => parameter.Name == "kind")?.Schema;
                if (kindSchema is OpenApiSchema schema) {
                    schema.Enum = [JsonValue.Create(CatalogInputValidator.EnumReferenceKind), JsonValue.Create(CatalogInputValidator.HelperReferenceKind)];
                }

                var scopeSchema = operation.Parameters?.SingleOrDefault(parameter => parameter.Name == "scope")?.Schema;
                if (scopeSchema is OpenApiSchema scope) {
                    scope.Enum = [JsonValue.Create(CatalogInputValidator.ComponentApiReferenceScope), JsonValue.Create(CatalogInputValidator.LibraryApiReferenceScope)];
                }

                return Task.CompletedTask;
            });

        api.MapGet("/references/{name}", (NTComponentsCatalog catalog,
            [Description("Reference type name or full name, for example NTButtonVariant or NTComponents.NTButtonVariant."), Required, MinLength(1)] string name) =>
            catalog.GetReference(name) is { } reference ? Results.Ok(reference) : Results.NotFound(new ErrorResponse($"Reference type '{name}' was not found.")))
            .WithName("GetNTReferenceType")
            .WithSummary("Get one NTComponents enum or helper")
            .WithDescription("Returns generated documentation, enum values or public members, obsolescence information, and the components that use one reference type.")
            .Produces<ReferenceDetails>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        api.MapGet("/search", (NTComponentsCatalog catalog,
            [Description("Required text matched against component, enum, and helper documentation."), Required, MinLength(1), MaxLength(CatalogInputValidator.MaximumQueryLength)] string query,
            [Description("Maximum number of results from 1 through 200."), Range(CatalogInputValidator.MinimumLimit, CatalogInputValidator.MaximumLimit)] int limit = 25,
            [Description("Zero-based result offset."), Range(0, int.MaxValue)] int offset = 0) => TypedResults.Ok(catalog.SearchPage(query, limit, offset)))
            .WithName("SearchNTComponents")
            .WithSummary("Search NTComponents documentation")
            .WithDescription("Returns relevance-ranked component, enum, and helper matches from the generated documentation catalog.")
            .Produces<DocumentationSearchPage>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static IResult GetHealth(IServiceProvider services, ILoggerFactory loggerFactory) {
        try {
            var overview = services.GetRequiredService<NTComponentsCatalog>().GetOverview();
            if (overview.ComponentCount > 0 && overview.ReferenceTypeCount > 0) {
                return TypedResults.Ok(new HealthStatus("Healthy"));
            }

            loggerFactory.CreateLogger("NTComponents.MCP.Health").LogError(
                "Catalog health check found {ComponentCount} components and {ReferenceTypeCount} reference types.",
                overview.ComponentCount,
                overview.ReferenceTypeCount);
        }
        catch (Exception exception) {
            loggerFactory.CreateLogger("NTComponents.MCP.Health").LogError(exception, "Catalog health check failed.");
        }

        return Results.Json(new HealthStatus("Unhealthy"), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
