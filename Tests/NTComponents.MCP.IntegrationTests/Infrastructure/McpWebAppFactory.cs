using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NTComponents.MCP.IntegrationTests.Infrastructure;

internal sealed class McpWebAppFactory(Action<IServiceCollection>? _configureServices = null) : WebApplicationFactory<Program> {
    private IHost? _host;
    private string? _serverAddress;

    public new HttpClient CreateClient() {
        EnsureHost();
        return new HttpClient { BaseAddress = new Uri(_serverAddress!) };
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureWebHost(webBuilder => {
            webBuilder.UseKestrel();
            webBuilder.UseUrls("http://127.0.0.1:0");
        });
        builder.ConfigureServices(services => _configureServices?.Invoke(services));

        var host = builder.Build();
        host.Start();
        _host = host;
        return host;
    }

    protected override void Dispose(bool disposing) {
        _host?.Dispose();
        base.Dispose(disposing);
    }

    private void EnsureHost() {
        if (_host is not null) {
            return;
        }

        try {
            _ = base.Services;
        }
        catch (InvalidCastException) {
            // WebApplicationFactory expects TestServer; this fixture intentionally hosts with Kestrel.
        }

        var server = _host?.Services.GetRequiredService<IServer>() ?? throw new InvalidOperationException("The MCP test host was not created.");
        _serverAddress = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single() ?? throw new InvalidOperationException("The MCP test host did not publish an address.");
    }
}
