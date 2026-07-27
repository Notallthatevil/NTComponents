using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace NTComponents.MCP.Tests.Hosting;

public class OriginPolicy_Tests {
    /// <summary>Behavior source: deployment/raspberry-pi/README.md documents exact single-origin matching for browser MCP requests; contradictory multiple Origin values must be rejected.</summary>
    [Fact]
    public void WithMultipleOriginValues_RejectsBrowserRequest() {
        var context = CreateContext();
        context.Request.Headers.Origin = new StringValues(["https://ntcomponents.nttechnologies.dev", "https://mcp.ntcomponents.nttechnologies.dev"]);

        var allowed = IsAllowedMcpOrigin(context, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https://ntcomponents.nttechnologies.dev" });

        allowed.Should().BeFalse();
    }

    /// <summary>Behavior source: deployment/raspberry-pi/README.md permits only same-origin or exact configured HTTP(S) browser origins, so malformed and non-origin values must be rejected.</summary>
    [Theory]
    [InlineData("not-an-origin")]
    [InlineData("ftp://attacker.example")]
    [InlineData("https://user@attacker.example")]
    [InlineData("https://attacker.example/path")]
    [InlineData("https://attacker.example?query=value")]
    [InlineData("https://attacker.example#fragment")]
    public void WithMalformedOrUnsupportedOrigin_RejectsBrowserRequest(string origin) {
        var context = CreateContext();
        context.Request.Headers.Origin = origin;

        var allowed = IsAllowedMcpOrigin(context, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        allowed.Should().BeFalse();
    }

    /// <summary>Behavior source: deployment/raspberry-pi/README.md explicitly permits a browser origin that exactly matches the MCP request origin.</summary>
    [Fact]
    public void WithSameOrigin_AllowsBrowserRequest() {
        var context = CreateContext();
        context.Request.Headers.Origin = "https://mcp.ntcomponents.nttechnologies.dev";

        var allowed = IsAllowedMcpOrigin(context, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        allowed.Should().BeTrue();
    }

    /// <summary>Behavior source: deployment/raspberry-pi/README.md permits configured browser origins only as exact HTTP(S) origins, so an invalid configured value must prevent unsafe startup.</summary>
    [Theory]
    [InlineData("ftp://attacker.example")]
    [InlineData("https://attacker.example/path")]
    public async Task WithInvalidConfiguredOrigin_StartupFailsWithActionableError(string origin) {
        var action = async () => {
            var entryPoint = typeof(Program).GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Static, [typeof(string[])])
                ?? throw new InvalidOperationException("The MCP application entry point was not found.");
            await (Task)entryPoint.Invoke(null, [new[] { $"--Mcp:AllowedOrigins:0={origin}" }])!;
        };

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{origin}*");
    }

    /// <summary>Behavior source: the public Problem Details contract preserves an explicit bad-request status while sanitizing unexpected server failures as HTTP 500.</summary>
    [Fact]
    public void ExceptionStatusContract_DistinguishesBadRequestsFromServerFailures() {
        var selector = typeof(Program).GetNestedTypes(BindingFlags.NonPublic)
            .SelectMany(type => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            .Single(method => method.ReturnType == typeof(int) && method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(Exception));
        var target = Activator.CreateInstance(selector.DeclaringType!, nonPublic: true);

        var badRequestStatus = (int)selector.Invoke(target, [new BadHttpRequestException("Payload too large.", StatusCodes.Status413PayloadTooLarge)])!;
        var serverFailureStatus = (int)selector.Invoke(target, [new InvalidOperationException("Sensitive failure detail.")])!;

        badRequestStatus.Should().Be(StatusCodes.Status413PayloadTooLarge);
        serverFailureStatus.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private static DefaultHttpContext CreateContext() {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("mcp.ntcomponents.nttechnologies.dev");
        return context;
    }

    private static bool IsAllowedMcpOrigin(HttpContext context, IReadOnlySet<string> allowedOrigins) {
        var method = typeof(Program).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(candidate => candidate.Name.Contains("IsAllowedMcpOrigin", StringComparison.Ordinal));
        return (bool)method.Invoke(null, [context, allowedOrigins])!;
    }
}
