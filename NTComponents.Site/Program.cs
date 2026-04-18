using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NTComponents.Site;
using NTComponents.Site.Documentation;
using NTComponents.Site.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
builder.Services.AddSassCompiler();
#endif

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<DocumentationCatalog>(_ => DocumentationCatalog.Default);
builder.Services.AddSingleton<MaterialThemeCssConverter>();

await builder.Build().RunAsync();

public partial class Program { }
