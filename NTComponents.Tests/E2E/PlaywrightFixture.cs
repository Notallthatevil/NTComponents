using Microsoft.Playwright;

namespace NTComponents.Tests.E2E;

/// <summary>
///     Base fixture for E2E tests using Playwright. Handles browser, context, and page lifecycle management with a test server using WebApplicationFactory.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime {
    private static readonly Lazy<Task<SharedPlaywrightResources>> SharedResources = new(CreateSharedResourcesAsync);

    public HttpClient HttpClient => new() { BaseAddress = new Uri(ServerAddress) };
    public string ServerAddress => _resources?.Factory.ServerAddress ?? throw new InvalidOperationException("Web application factory is not initialized.");
    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    private SharedPlaywrightResources? _resources;

    static PlaywrightFixture() {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeSharedResources();
    }

    /// <summary>
    ///     Clean up browser and application resources after test.
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (Page != null) {
            await Page.CloseAsync();
        }

        if (Context != null) {
            await Context.CloseAsync();
        }

        _resources = null;
    }

    /// <summary>
    ///     Initialize the test application server and Playwright browser.
    /// </summary>
    public async ValueTask InitializeAsync() {
        _resources = await SharedResources.Value;
        Context = await _resources.Browser.NewContextAsync();
        Page = await Context.NewPageAsync();
    }

    private static async Task<SharedPlaywrightResources> CreateSharedResourcesAsync() {
        var factory = new NTWebAppFactory();
        _ = factory.Services;

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() {
            Headless = true,
        });

        return new(factory, playwright, browser);
    }

    private static void DisposeSharedResources() {
        if (!SharedResources.IsValueCreated || !SharedResources.Value.IsCompletedSuccessfully) {
            return;
        }

        var resources = SharedResources.Value.Result;
        resources.Browser.CloseAsync().GetAwaiter().GetResult();
        resources.Playwright.Dispose();
        resources.Factory.Dispose();
    }

    private sealed record SharedPlaywrightResources(NTWebAppFactory Factory, IPlaywright Playwright, IBrowser Browser);
}
