using NTComponents.MCP.Catalog;
using NTComponents.MCP.Endpoints;
using NTComponents.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<NTComponentsCatalog>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<NTComponentsTools>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapNTComponentsEndpoints();
app.MapMcp("/mcp");

await app.RunAsync();

public partial class Program;
