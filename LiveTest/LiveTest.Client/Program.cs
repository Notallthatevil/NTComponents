using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddLogging();

builder.Services.AddScoped(sp =>
    new HttpClient {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddNTServices();

await builder.Build().RunAsync();
