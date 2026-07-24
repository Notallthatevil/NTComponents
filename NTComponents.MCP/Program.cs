using Microsoft.AspNetCore.HttpOverrides;
using NTComponents.MCP.Catalog;
using NTComponents.MCP.Endpoints;
using NTComponents.MCP.Resources;
using NTComponents.MCP.Tools;
using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;

const long MaxRequestBodySize = 64 * 1024;
const int RequestsPerMinute = 60;
const string McpCorsPolicy = "McpBrowserClients";

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var allowedMcpOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var configuredOrigin in builder.Configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? []) {
    if (!TryNormalizeOrigin(configuredOrigin, out var normalizedOrigin)) {
        throw new InvalidOperationException($"Configured MCP origin '{configuredOrigin}' is not an HTTP or HTTPS origin.");
    }

    allowedMcpOrigins.Add(normalizedOrigin);
}

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestBodySize);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy(McpCorsPolicy, policy => {
    if (allowedMcpOrigins.Count > 0) {
        policy.WithOrigins([.. allowedMcpOrigins]);
    }

    policy.WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Delete)
        .WithHeaders("Accept", "Content-Type", "MCP-Protocol-Version", "Mcp-Session-Id", "Last-Event-ID")
        .WithExposedHeaders("Mcp-Session-Id")
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
}));
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
        if (string.Equals(context.Request.Path.Value, "/health", StringComparison.OrdinalIgnoreCase)) {
            return RateLimitPartition.GetNoLimiter("health");
        }

        return RateLimitPartition.GetSlidingWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions {
                AutoReplenishment = false,
                PermitLimit = RequestsPerMinute,
                QueueLimit = 0,
                SegmentsPerWindow = 6,
                Window = TimeSpan.FromMinutes(1),
            });
    });
    options.OnRejected = (context, _) => {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterMetadata) ? retryAfterMetadata : TimeSpan.FromMinutes(1);
        context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        return ValueTask.CompletedTask;
    };
});
builder.Services.AddSingleton<NTComponentsCatalog>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<NTComponentsTools>()
    .WithResources<NTComponentsResources>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRouting();
var requestLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("NTComponents.MCP.Requests");
app.Use(async (context, next) => {
    var startedAt = Stopwatch.GetTimestamp();
    try {
        await next(context);
    } finally {
        requestLogger.LogInformation(
            "HTTP {Method} {Endpoint} responded {StatusCode} in {ElapsedMilliseconds} ms with trace {TraceId}",
            context.Request.Method,
            context.GetEndpoint()?.DisplayName ?? "Unmatched",
            context.Response.StatusCode,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            context.TraceIdentifier);
    }
});
app.UseExceptionHandler(new ExceptionHandlerOptions {
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequestException ? badRequestException.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseRateLimiter();
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/mcp") && !IsAllowedMcpOrigin(context, allowedMcpOrigins)) {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next(context);
});
app.UseCors();
app.MapOpenApi();
app.MapNTComponentsEndpoints();
app.MapMcp("/mcp")
    .WithName("NTComponentsMcp")
    .WithSummary("NTComponents Model Context Protocol endpoint")
    .WithDescription("Connect an MCP client here to discover and call the NTComponents documentation tools.")
    .WithTags("MCP")
    .RequireCors(McpCorsPolicy);

await app.RunAsync();

static bool IsAllowedMcpOrigin(HttpContext context, IReadOnlySet<string> allowedOrigins) {
    var originHeaders = context.Request.Headers.Origin;
    if (originHeaders.Count == 0) {
        return true;
    }

    if (originHeaders.Count != 1 || !TryNormalizeOrigin(originHeaders[0], out var origin)) {
        return false;
    }

    if (allowedOrigins.Contains(origin)) {
        return true;
    }

    return TryNormalizeOrigin($"{context.Request.Scheme}://{context.Request.Host}", out var requestOrigin)
        && string.Equals(origin, requestOrigin, StringComparison.OrdinalIgnoreCase);
}

static bool TryNormalizeOrigin(string? value, out string origin) {
    origin = string.Empty;
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        || !string.IsNullOrEmpty(uri.UserInfo)
        || uri.AbsolutePath != "/"
        || !string.IsNullOrEmpty(uri.Query)
        || !string.IsNullOrEmpty(uri.Fragment)) {
        return false;
    }

    origin = uri.GetLeftPart(UriPartial.Authority);
    return true;
}

public partial class Program;
