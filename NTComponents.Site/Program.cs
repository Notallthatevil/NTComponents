using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NTComponents.Site;
using NTComponents.Site.Documentation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddNTServices();
// The documentation site is WebAssembly-only, so every log belongs to the person viewing it and the debug console can stay enabled for everyone.
builder.Services.AddNTDebugConsole(options => options.AllowAll());
builder.Services.AddSingleton<DocumentationCatalog>();

await builder.Build().RunAsync();

public partial class Program { }
