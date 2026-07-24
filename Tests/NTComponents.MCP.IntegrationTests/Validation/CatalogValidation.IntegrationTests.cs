using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NTComponents.MCP.IntegrationTests.Infrastructure;
using System.Net;
using System.Text.Json;

namespace NTComponents.MCP.IntegrationTests.Validation;

public class CatalogValidation_Tests {
    /// <summary>Behavior source: REST parameter descriptions define the inclusive limit range as 1 through 200.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public async Task Rest_WithBoundaryLimit_ReturnsSuccess(int limit) {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        using var components = await client.GetAsync($"/api/components?limit={limit}", TestContext.Current.CancellationToken);
        using var references = await client.GetAsync($"/api/references?limit={limit}", TestContext.Current.CancellationToken);
        using var search = await client.GetAsync($"/api/search?query=button&limit={limit}", TestContext.Current.CancellationToken);

        components.StatusCode.Should().Be(HttpStatusCode.OK);
        references.StatusCode.Should().Be(HttpStatusCode.OK);
        search.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Behavior source: REST parameter descriptions reject limits outside 1 through 200 rather than silently normalizing them.</summary>
    [Theory]
    [InlineData("/api/components?limit=0")]
    [InlineData("/api/components?limit=201")]
    [InlineData("/api/references?limit=0")]
    [InlineData("/api/references?limit=201")]
    [InlineData("/api/search?query=button&limit=0")]
    [InlineData("/api/search?query=button&limit=201")]
    public async Task Rest_WithOutOfRangeLimit_ReturnsValidationProblem(string path) {
        await AssertRestValidationProblemAsync(path, "limit");
    }

    /// <summary>Behavior source: the reference endpoint documents Enum and Helper and preserves case-insensitive matching.</summary>
    [Theory]
    [InlineData("Enum")]
    [InlineData("enum")]
    [InlineData("Helper")]
    [InlineData("helper")]
    public async Task Rest_WithDocumentedReferenceKind_ReturnsSuccess(string kind) {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/references?kind={kind}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Behavior source: the reference endpoint defines Enum and Helper as its closed set of supplied kind values.</summary>
    [Theory]
    [InlineData("Widget")]
    [InlineData("%20")]
    public async Task Rest_WithInvalidReferenceKind_ReturnsValidationProblem(string kind) {
        await AssertRestValidationProblemAsync($"/api/references?kind={kind}", "kind");
    }

    /// <summary>Behavior source: the reference endpoint defines ComponentApi and LibraryApi as its closed set of supplied scope values.</summary>
    [Theory]
    [InlineData("Widget")]
    [InlineData("%20")]
    public async Task Rest_WithInvalidReferenceScope_ReturnsValidationProblem(string scope) {
        await AssertRestValidationProblemAsync($"/api/references?scope={scope}", "scope");
    }

    /// <summary>Behavior source: paging offsets are zero-based and cannot be negative.</summary>
    [Theory]
    [InlineData("/api/components?offset=-1")]
    [InlineData("/api/references?offset=-1")]
    [InlineData("/api/search?query=button&offset=-1")]
    public async Task Rest_WithNegativeOffset_ReturnsValidationProblem(string path) {
        await AssertRestValidationProblemAsync(path, "offset");
    }

    /// <summary>Behavior source: the search endpoint documents query as required search text.</summary>
    [Fact]
    public async Task Rest_WithBlankSearchQuery_ReturnsValidationProblem() {
        await AssertRestValidationProblemAsync("/api/search?query=%20", "query");
    }

    /// <summary>Behavior source: REST catalog query parameters accept at most 512 characters.</summary>
    [Fact]
    public async Task Rest_WithQueryLengthBoundary_Accepts512AndRejects513() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        var maximumQuery = new string('a', 512);
        var excessiveQuery = new string('a', 513);

        foreach (var path in new[] { "/api/components", "/api/references", "/api/search" }) {
            using var accepted = await client.GetAsync($"{path}?query={maximumQuery}", TestContext.Current.CancellationToken);
            using var rejected = await client.GetAsync($"{path}?query={excessiveQuery}", TestContext.Current.CancellationToken);

            accepted.StatusCode.Should().Be(HttpStatusCode.OK);
            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    /// <summary>Behavior source: component and reference lookup endpoints require a nonblank type name.</summary>
    [Theory]
    [InlineData("/api/components/%20")]
    [InlineData("/api/references/%20")]
    public async Task Rest_WithBlankLookupName_ReturnsValidationProblem(string path) {
        await AssertRestValidationProblemAsync(path, "name");
    }

    /// <summary>Behavior source: a syntactically valid unknown component or reference name uses the existing not-found contract.</summary>
    [Theory]
    [InlineData("/api/components/NTDoesNotExist")]
    [InlineData("/api/references/NTDoesNotExist")]
    public async Task Rest_WithValidUnknownName_ReturnsNotFound(string path) {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Behavior source: every REST operation whose catalog call can reject input advertises HTTP 400 and query-length constraints in OpenAPI.</summary>
    [Fact]
    public async Task OpenApi_AdvertisesValidationResponsesAndQueryLength() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in new[] { "/api/components", "/api/components/{name}", "/api/references", "/api/references/{name}", "/api/search" }) {
            paths.GetProperty(path).GetProperty("get").GetProperty("responses").TryGetProperty("400", out _).Should().BeTrue();
        }

        AssertOpenApiQueryLength(paths.GetProperty("/api/components").GetProperty("get"));
        AssertOpenApiQueryLength(paths.GetProperty("/api/references").GetProperty("get"));
        AssertOpenApiQueryLength(paths.GetProperty("/api/search").GetProperty("get"));
        AssertOpenApiAllowedKinds(paths.GetProperty("/api/references").GetProperty("get"));
    }

    /// <summary>Behavior source: MCP parameter contracts publish range, allowed-value, and required-input constraints to clients.</summary>
    [Fact]
    public async Task McpToolSchemas_AdvertiseInputConstraints() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var componentListSchema = tools.Single(tool => tool.Name == "list_nt_components").JsonSchema;
        var referenceListSchema = tools.Single(tool => tool.Name == "list_nt_reference_types").JsonSchema;
        var componentLookupSchema = tools.Single(tool => tool.Name == "get_nt_component").JsonSchema;
        var referenceLookupSchema = tools.Single(tool => tool.Name == "get_nt_reference_type").JsonSchema;
        var searchSchema = tools.Single(tool => tool.Name == "search_ntcomponents").JsonSchema;

        AssertLimitSchema(componentListSchema);
        AssertLimitSchema(referenceListSchema);
        AssertLimitSchema(searchSchema);
        AssertQueryLengthSchema(componentListSchema);
        AssertQueryLengthSchema(referenceListSchema);
        AssertQueryLengthSchema(searchSchema);
        AssertOffsetSchema(componentListSchema);
        AssertOffsetSchema(referenceListSchema);
        AssertOffsetSchema(searchSchema);
        referenceListSchema.GetProperty("properties").GetProperty("kind").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).Should().Equal("Enum", "Helper");
        referenceListSchema.GetProperty("properties").GetProperty("scope").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).Should().Equal("ComponentApi", "LibraryApi");
        componentLookupSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).Should().Contain("name");
        referenceLookupSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).Should().Contain("name");
        searchSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).Should().Contain("query");
        componentLookupSchema.GetProperty("properties").GetProperty("name").GetProperty("minLength").GetInt32().Should().Be(1);
        referenceLookupSchema.GetProperty("properties").GetProperty("name").GetProperty("minLength").GetInt32().Should().Be(1);
        searchSchema.GetProperty("properties").GetProperty("query").GetProperty("minLength").GetInt32().Should().Be(1);
    }

    /// <summary>Behavior source: invalid MCP arguments return a tool error and do not corrupt the stateless session for a subsequent valid call.</summary>
    [Fact]
    public async Task McpClient_AfterInvalidLimit_ReturnsToolErrorThenValidResult() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var invalidResult = await client.CallToolAsync(
            "list_nt_components",
            new Dictionary<string, object?> { ["limit"] = 0 },
            cancellationToken: TestContext.Current.CancellationToken);
        var validResult = await client.CallToolAsync(
            "list_nt_components",
            new Dictionary<string, object?> { ["limit"] = 1 },
            cancellationToken: TestContext.Current.CancellationToken);

        invalidResult.IsError.Should().BeTrue();
        GetErrorText(invalidResult).Should().Contain("limit must be between 1 and 200.");
        validResult.IsError.Should().NotBeTrue();
        validResult.Content.Should().NotBeEmpty();
    }

    /// <summary>Behavior source: required MCP name/query inputs must be nonblank, and kind is limited to Enum or Helper.</summary>
    [Fact]
    public async Task McpClient_WithInvalidTextArguments_ReturnsToolErrors() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);
        var invalidCalls = new[] {
            (Tool: "get_nt_component", Arguments: new Dictionary<string, object?> { ["name"] = " " }, ExpectedMessage: "name is required and cannot be blank."),
            (Tool: "get_nt_reference_type", Arguments: new Dictionary<string, object?> { ["name"] = " " }, ExpectedMessage: "name is required and cannot be blank."),
            (Tool: "search_ntcomponents", Arguments: new Dictionary<string, object?> { ["query"] = " " }, ExpectedMessage: "query is required and cannot be blank."),
            (Tool: "list_nt_reference_types", Arguments: new Dictionary<string, object?> { ["kind"] = "Widget" }, ExpectedMessage: "kind must be Enum or Helper."),
            (Tool: "list_nt_reference_types", Arguments: new Dictionary<string, object?> { ["kind"] = " " }, ExpectedMessage: "kind must be Enum or Helper."),
            (Tool: "list_nt_reference_types", Arguments: new Dictionary<string, object?> { ["scope"] = "Widget" }, ExpectedMessage: "scope must be ComponentApi or LibraryApi."),
            (Tool: "list_nt_components", Arguments: new Dictionary<string, object?> { ["offset"] = -1 }, ExpectedMessage: "offset must be zero or greater."),
        };

        foreach (var invalidCall in invalidCalls) {
            var result = await client.CallToolAsync(invalidCall.Tool, invalidCall.Arguments, cancellationToken: TestContext.Current.CancellationToken);

            result.IsError.Should().BeTrue();
            GetErrorText(result).Should().Contain(invalidCall.ExpectedMessage);
        }
    }

    /// <summary>Behavior source: MCP catalog query parameters accept 512 characters and reject 513 characters.</summary>
    [Fact]
    public async Task McpClient_WithQueryLengthBoundary_Accepts512AndRejects513() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);
        var maximumQuery = new string('a', 512);
        var excessiveQuery = new string('a', 513);

        foreach (var tool in new[] { "list_nt_components", "list_nt_reference_types", "search_ntcomponents" }) {
            var accepted = await client.CallToolAsync(tool, new Dictionary<string, object?> { ["query"] = maximumQuery }, cancellationToken: TestContext.Current.CancellationToken);
            var rejected = await client.CallToolAsync(tool, new Dictionary<string, object?> { ["query"] = excessiveQuery }, cancellationToken: TestContext.Current.CancellationToken);

            accepted.IsError.Should().NotBeTrue();
            rejected.IsError.Should().BeTrue();
            GetErrorText(rejected).Should().Contain("query cannot exceed 512 characters.");
        }
    }

    /// <summary>Behavior source: the live MCP protocol advertises safe tool hints and structured output schemas for every documentation operation.</summary>
    [Fact]
    public async Task McpTools_AdvertiseSafeAnnotationsAndStructuredOutput() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        tools.Select(tool => tool.Name).Should().BeEquivalentTo("get_nt_catalog_overview", "list_nt_components", "get_nt_component", "list_nt_reference_types", "get_nt_reference_type", "search_ntcomponents");
        foreach (var tool in tools) {
            tool.ProtocolTool.Annotations.Should().NotBeNull();
            tool.ProtocolTool.Annotations!.ReadOnlyHint.Should().BeTrue();
            tool.ProtocolTool.Annotations.IdempotentHint.Should().BeTrue();
            tool.ProtocolTool.Annotations.DestructiveHint.Should().BeFalse();
            tool.ProtocolTool.Annotations.OpenWorldHint.Should().BeFalse();
            tool.ProtocolTool.OutputSchema.Should().NotBeNull();
        }
    }

    /// <summary>Behavior source: paged MCP calls expose machine-readable totals and continuation metadata instead of silently truncating text-only arrays.</summary>
    [Fact]
    public async Task McpListReferenceTypes_ReturnsStructuredPagingMetadata() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var result = await client.CallToolAsync("list_nt_reference_types", cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.Should().NotBeTrue();
        result.StructuredContent.Should().NotBeNull();
        var page = result.StructuredContent!.Value;
        page.GetProperty("items").GetArrayLength().Should().Be(100);
        page.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(100);
        page.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        page.GetProperty("nextOffset").GetInt32().Should().Be(100);
    }

    /// <summary>Behavior source: MCP resources make the catalog and individual component/reference documentation directly addressable.</summary>
    [Fact]
    public async Task McpResources_AdvertiseAndReadCatalogDocumentation() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);
        var component = await client.ReadResourceAsync("ntcomponents://components/NTAccordion", cancellationToken: TestContext.Current.CancellationToken);

        resources.Should().Contain(resource => resource.Uri == "ntcomponents://catalog");
        templates.Should().Contain(template => template.UriTemplate == "ntcomponents://components/{name}");
        templates.Should().Contain(template => template.UriTemplate == "ntcomponents://references/{name}");
        component.Contents.OfType<TextResourceContents>().Single().Text.Should().Contain("NTAccordionItem");
    }

    private static async Task AssertRestValidationProblemAsync(string path, string parameterName) {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("errors").TryGetProperty(parameterName, out _).Should().BeTrue();
    }

    private static async Task<McpClient> CreateMcpClientAsync(McpWebAppFactory factory) {
        using var httpClient = factory.CreateClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions {
            Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        });
        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static void AssertLimitSchema(JsonElement schema) {
        var limit = schema.GetProperty("properties").GetProperty("limit");
        limit.GetProperty("minimum").GetInt32().Should().Be(1);
        limit.GetProperty("maximum").GetInt32().Should().Be(200);
    }

    private static void AssertQueryLengthSchema(JsonElement schema) =>
        schema.GetProperty("properties").GetProperty("query").GetProperty("maxLength").GetInt32().Should().Be(512);

    private static void AssertOffsetSchema(JsonElement schema) =>
        schema.GetProperty("properties").GetProperty("offset").GetProperty("minimum").GetInt32().Should().Be(0);

    private static void AssertOpenApiQueryLength(JsonElement operation) {
        var query = operation.GetProperty("parameters").EnumerateArray().Single(parameter => parameter.GetProperty("name").GetString() == "query");
        query.GetProperty("schema").GetProperty("maxLength").GetInt32().Should().Be(512);
    }

    private static void AssertOpenApiAllowedKinds(JsonElement operation) {
        var kind = operation.GetProperty("parameters").EnumerateArray().Single(parameter => parameter.GetProperty("name").GetString() == "kind");
        kind.GetProperty("schema").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).Should().Equal("Enum", "Helper");
    }

    private static string GetErrorText(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(content => content.Text));
}
