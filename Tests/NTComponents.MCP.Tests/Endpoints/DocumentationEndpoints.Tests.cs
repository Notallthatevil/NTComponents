using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Endpoints;

namespace NTComponents.MCP.Tests.Endpoints;

public class DocumentationEndpoints_Tests {
    /// <summary>Behavior source: the service discovery endpoint description promises MCP, OpenAPI, health, REST API locations, and current catalog counts.</summary>
    [Fact]
    public async Task Discovery_ReturnsDocumentedLocationsAndNonemptyCatalog() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/");

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("name").GetString().Should().Be("NTComponents.MCP");
        response.Json.RootElement.GetProperty("mcp").GetString().Should().Be("/mcp");
        response.Json.RootElement.GetProperty("openApi").GetString().Should().Be("/openapi/v1.json");
        response.Json.RootElement.GetProperty("health").GetString().Should().Be("/health");
        response.Json.RootElement.GetProperty("api").GetString().Should().Be("/api");
        response.Json.RootElement.GetProperty("catalog").GetProperty("componentCount").GetInt32().Should().BeGreaterThan(0);
        response.Json.RootElement.GetProperty("catalog").GetProperty("referenceTypeCount").GetInt32().Should().BeGreaterThan(0);
    }

    /// <summary>Behavior source: the health endpoint description defines healthy as a resolvable catalog containing both components and reference types.</summary>
    [Fact]
    public async Task Health_WithAvailableCatalog_ReturnsHealthyStatus() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/health");

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    /// <summary>Behavior source: the health endpoint's documented 503 contract returns HealthStatus when the documentation catalog cannot be resolved.</summary>
    [Fact]
    public async Task Health_WithUnavailableCatalog_ReturnsSanitizedUnhealthyResponse() {
        await using var app = CreateApplication(catalogUnavailable: true);

        var response = await InvokeAsync(app, "/health");

        response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        response.Json.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("status");
        response.Json.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
    }

    /// <summary>Behavior source: the component-list endpoint description promises a paged list of public NT-prefixed components and the endpoint parameters define limit and offset.</summary>
    [Fact]
    public async Task Components_WithPaging_ReturnsRequestedPublicSliceAndPagingMetadata() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/components", "?folder=Buttons&limit=2&offset=1");

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        response.Json.RootElement.GetProperty("items").EnumerateArray().Should().OnlyContain(item => item.GetProperty("name").GetString()!.StartsWith("NT", StringComparison.Ordinal));
        response.Json.RootElement.GetProperty("items").EnumerateArray().Should().OnlyContain(item => item.GetProperty("folder").GetString() == "Buttons");
        response.Json.RootElement.GetProperty("offset").GetInt32().Should().Be(1);
        response.Json.RootElement.GetProperty("limit").GetInt32().Should().Be(2);
        response.Json.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        response.Json.RootElement.GetProperty("nextOffset").GetInt32().Should().Be(3);
    }

    /// <summary>Behavior source: the component-detail endpoint description promises usage guidance, parameters, methods, render compatibility, related types, and obsolescence for a simple or full type name.</summary>
    [Theory]
    [InlineData("NTButton")]
    [InlineData("NTComponents.NTButton")]
    public async Task Component_WithDocumentedNameForms_ReturnsStructuredDetails(string name) {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/components/{name}", routeName: name);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("name").GetString().Should().Be("NTButton");
        response.Json.RootElement.GetProperty("parameters").GetArrayLength().Should().BeGreaterThan(0);
        response.Json.RootElement.GetProperty("razorUsage").GetString().Should().Contain("<NTButton");
        response.Json.RootElement.GetProperty("documentationUrl").GetString().Should().Be("https://ntcomponents.nttechnologies.dev/components/ntbutton");
    }

    /// <summary>Behavior source: the component-detail endpoint declares an ErrorResponse 404 contract for a valid name that is absent from the catalog.</summary>
    [Fact]
    public async Task Component_WithUnknownName_ReturnsSpecificNotFoundError() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/components/{name}", routeName: "NTDoesNotExist");

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Behavior source: the reference-list endpoint defines Enum and ComponentApi as supported filters and promises a paged reference summary response.</summary>
    [Fact]
    public async Task References_WithDocumentedFilters_ReturnsMatchingPage() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/references", "?kind=enum&scope=componentapi&limit=5");

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("items").GetArrayLength().Should().Be(5);
        response.Json.RootElement.GetProperty("items").EnumerateArray().Should().OnlyContain(item => item.GetProperty("kind").GetString() == "Enum");
        response.Json.RootElement.GetProperty("items").EnumerateArray().Should().OnlyContain(item => item.GetProperty("scope").GetString() == "ComponentApi");
        response.Json.RootElement.GetProperty("limit").GetInt32().Should().Be(5);
    }

    /// <summary>Behavior source: the reference-detail endpoint description promises enum values or public helper members plus component usage for a simple or full type name.</summary>
    [Theory]
    [InlineData("NTButtonVariant")]
    [InlineData("NTComponents.NTButtonVariant")]
    public async Task Reference_WithDocumentedNameForms_ReturnsStructuredDetails(string name) {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/references/{name}", routeName: name);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("name").GetString().Should().Be("NTButtonVariant");
        response.Json.RootElement.GetProperty("kind").GetString().Should().Be("Enum");
        response.Json.RootElement.GetProperty("fields").GetArrayLength().Should().BeGreaterThan(0);
        response.Json.RootElement.GetProperty("usedByComponents").EnumerateArray().Select(item => item.GetString()).Should().Contain("NTButton");
    }

    /// <summary>Behavior source: the reference-detail endpoint declares an ErrorResponse 404 contract for a valid name that is absent from the catalog.</summary>
    [Fact]
    public async Task Reference_WithUnknownName_ReturnsSpecificNotFoundError() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/references/{name}", routeName: "NTDoesNotExist");

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Behavior source: the search endpoint description promises relevance-ranked component, enum, and helper matches with limit and offset paging.</summary>
    [Fact]
    public async Task Search_WithDocumentedQuery_ReturnsRankedResultContract() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/search", "?query=dialog%20elevation&limit=2");

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Json.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        response.Json.RootElement.GetProperty("items")[0].GetProperty("name").GetString().Should().Be("NTDialog");
        response.Json.RootElement.GetProperty("items")[0].GetProperty("matchedTerms").GetArrayLength().Should().Be(2);
        response.Json.RootElement.GetProperty("items")[0].GetProperty("documentationUrl").GetString().Should().StartWith("https://ntcomponents.nttechnologies.dev/");
    }

    /// <summary>Behavior source: REST endpoints declare validation-problem responses and the public limit contract accepts only values from 1 through 200.</summary>
    [Fact]
    public async Task Components_WithInvalidLimit_ReturnsActionableValidationProblem() {
        await using var app = CreateApplication();

        var response = await InvokeAsync(app, "/api/components", "?limit=0");

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
        response.Json.RootElement.GetProperty("errors").GetProperty("limit")[0].GetString().Should().Be("limit must be between 1 and 200.");
    }

    private static WebApplication CreateApplication(bool catalogUnavailable = false) {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddRouting();
        builder.Services.AddProblemDetails();
        if (catalogUnavailable) {
            builder.Services.AddSingleton<NTComponentsCatalog>(_ => throw new InvalidOperationException("Sensitive catalog failure detail."));
        }
        else {
            builder.Services.AddSingleton<NTComponentsCatalog>();
        }

        var app = builder.Build();
        app.MapNTComponentsEndpoints();
        return app;
    }

    private static async Task<EndpointResponse> InvokeAsync(WebApplication app, string routePattern, string? query = null, string? routeName = null) {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == routePattern);
        var context = new DefaultHttpContext {
            RequestServices = app.Services,
            Response = { Body = new MemoryStream() },
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString(query);
        if (routeName is not null) {
            context.Request.RouteValues["name"] = routeName;
        }

        var requestDelegate = endpoint.RequestDelegate ?? throw new InvalidOperationException($"Route '{routePattern}' did not produce a request delegate.");
        await requestDelegate(context);
        context.Response.Body.Position = 0;
        return new(context.Response.StatusCode, await JsonDocument.ParseAsync(context.Response.Body));
    }

    private sealed record EndpointResponse(int StatusCode, JsonDocument Json);
}
