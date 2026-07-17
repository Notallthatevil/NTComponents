using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace NTComponents.IntegrationTests;

public class NTWebAppFactory : WebApplicationFactory<Program> {
    private IHost? _host;
    private string? _serverAddress;

    public string ServerAddress {
        get {
            EnsureHost();
            return _serverAddress!;
        }
    }

    public override IServiceProvider Services {
        get {
            EnsureHost();
            return _host!.Services;
        }
    }

    private void EnsureHost() {
        if (_host != null) return;

        try {
            // Trigger EnsureServer logic which calls CreateHost
            // We expect this to fail with InvalidCastException because we return a Kestrel host
            // but WebApplicationFactory expects TestServer.
            var _ = base.Services;
        } catch (InvalidCastException) {
            // Expected because this factory intentionally uses Kestrel instead of TestServer.
        }

        if (_host == null) {
            throw new InvalidOperationException("Host was not created.");
        }
        
        // Extract address
        var server = _host.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        _serverAddress = addressFeature!.Addresses.First();
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        builder.ConfigureLogging(logging => {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services => {
            var dataProtectionDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "NTComponents.IntegrationTests", "DataProtectionKeys", Environment.ProcessId.ToString()));
            services.AddDataProtection()
                .SetApplicationName("NTComponents.IntegrationTests.E2E")
                .PersistKeysToFileSystem(dataProtectionDirectory);
        });

        // Configure Kestrel
        builder.ConfigureWebHost(webBuilder => {
            webBuilder.UseKestrel();
            webBuilder.UseUrls("http://127.0.0.1:0");
        });

        var host = builder.Build();
        host.Start();
        _host = host;

        return host;
    }

    protected override void Dispose(bool disposing) {
        _host?.Dispose();
        base.Dispose(disposing);
    }
}
