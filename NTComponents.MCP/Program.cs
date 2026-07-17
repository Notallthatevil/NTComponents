using NTComponents.MCP.Catalog;
using NTComponents.MCP.Endpoints;
using NTComponents.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<NTComponentsCatalog>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<NTComponentsTools>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapOpenApi();
app.MapNTComponentsEndpoints();
app.MapMcp("/mcp")
    .WithName("NTComponentsMcp")
    .WithSummary("NTComponents Model Context Protocol endpoint")
    .WithDescription("Connect an MCP client here to discover and call the NTComponents documentation tools.")
    .WithTags("MCP");

await app.RunAsync();

public partial class Program;
