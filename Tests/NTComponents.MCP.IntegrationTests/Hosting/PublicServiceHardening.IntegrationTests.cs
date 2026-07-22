using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Contracts;
using NTComponents.MCP.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NTComponents.MCP.IntegrationTests.Hosting;

public class PublicServiceHardening_Tests {
    [Fact]
    public async Task WithAllowedPublicHost_ReturnsServiceDiscovery() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = "mcp.ntcomponents.nttechnologies.dev";

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithUnapprovedHost_RejectsRequest() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = "attacker.example";

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WithCrossOriginPreflight_DoesNotAllowBrowserOrigin() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/mcp");
        request.Headers.Add("Origin", "https://attacker.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task WithConfiguredOriginPreflight_AllowsMcpRequestContract() {
        const string origin = "https://ntcomponents.nttechnologies.dev";
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/mcp");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,mcp-protocol-version,mcp-session-id,last-event-id");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(origin);
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task WithHostileOrigin_RejectsValidMcpInitializeRequest() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = CreateInitializeRequest("https://attacker.example");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://mcp.ntcomponents.nttechnologies.dev")]
    public async Task WithAllowedOrigin_ProcessesMcpInitializeRequest(string? origin) {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = CreateInitializeRequest(origin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task WithConfiguredOrigin_ProcessesMcpInitializeRequestWithCorsHeader() {
        const string origin = "https://ntcomponents.nttechnologies.dev";
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = CreateInitializeRequest(origin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(origin);
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task AfterForwardedClientLimit_RejectsThatClientButAllowsAnotherClientAndHealth() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();

        for (var index = 0; index < 60; index++) {
            using var permittedRequest = CreateForwardedRequest("198.51.100.10", "/");
            using var permittedResponse = await client.SendAsync(permittedRequest, TestContext.Current.CancellationToken);
            permittedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var rejectedRequest = CreateForwardedRequest("198.51.100.10", "/");
        using var rejectedResponse = await client.SendAsync(rejectedRequest, TestContext.Current.CancellationToken);
        using var otherClientRequest = CreateForwardedRequest("198.51.100.11", "/");
        using var otherClientResponse = await client.SendAsync(otherClientRequest, TestContext.Current.CancellationToken);
        using var healthRequest = CreateForwardedRequest("198.51.100.10", "/health");
        using var healthResponse = await client.SendAsync(healthRequest, TestContext.Current.CancellationToken);

        rejectedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejectedResponse.Headers.RetryAfter.Should().NotBeNull();
        otherClientResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithBodyLargerThanSixtyFourKibibytes_RejectsMcpRequest() {
        await using var factory = new McpWebAppFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") {
            Content = new StringContent(new string('x', (64 * 1024) + 1)),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var healthResponse = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithCatalogResolutionFailure_ReturnsSanitizedUnhealthyResponse() {
        await using var factory = new McpWebAppFactory(services => {
            services.RemoveAll<NTComponentsCatalog>();
            services.AddSingleton<NTComponentsCatalog>(_ => throw new InvalidOperationException("Sensitive catalog failure detail."));
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var health = JsonSerializer.Deserialize<HealthStatus>(responseBody, JsonSerializerOptions.Web);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        health.Should().Be(new HealthStatus("Unhealthy"));
        responseBody.Should().NotContain("Sensitive catalog failure detail");
    }

    [Fact]
    public async Task WithDefaultHostLogging_InvalidMcpCallReturnsActionableToolError() {
        await using var factory = new McpWebAppFactory();
        await using var client = await CreateMcpClientAsync(factory);

        var result = await client.CallToolAsync(
            "list_nt_components",
            new Dictionary<string, object?> { ["limit"] = 0 },
            cancellationToken: TestContext.Current.CancellationToken);

        var errorText = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(content => content.Text));
        result.IsError.Should().BeTrue();
        errorText.Should().Contain("limit must be between 1 and 200.");
    }

    private static HttpRequestMessage CreateForwardedRequest(string clientIpAddress, string path) {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", clientIpAddress);
        return request;
    }

    private static HttpRequestMessage CreateInitializeRequest(string? origin) {
        const string initializeRequest = """
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"NTComponents.MCP.IntegrationTests","version":"1.0"}}}
            """;
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") {
            Content = new StringContent(initializeRequest, Encoding.UTF8, "application/json"),
        };
        request.Headers.Host = "mcp.ntcomponents.nttechnologies.dev";
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (origin is not null) {
            request.Headers.Add("Origin", origin);
        }

        return request;
    }

    private static async Task<McpClient> CreateMcpClientAsync(McpWebAppFactory factory) {
        using var httpClient = factory.CreateClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions {
            Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        });
        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }
}
