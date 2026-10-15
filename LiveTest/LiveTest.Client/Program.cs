using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddLogging();

builder.Services.AddScoped(sp =>
    new HttpClient {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddNTServices();
// WebAssembly runs one user's session, so all captured logs belong to the viewer.
builder.Services.AddNTDebugConsole(options => options.AllowAll());

await builder.Build().RunAsync();
