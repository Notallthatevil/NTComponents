using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Theming;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTThemeEnhancedNavigation_IntegrationTests : IAsyncLifetime {
    private readonly ConcurrentQueue<string> _requests = new();
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string _appBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        _appBaseUrl = _fixture.ServerAddress;
        _page.Request += (_, request) => _requests.Enqueue(request.Url);
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("DARK", "DEFAULT", "Light", "dark.css")]
    [InlineData("LIGHT", "MEDIUM", "Dark", "light-mc.css")]
    [InlineData("SYSTEM", "HIGH", "Dark", "dark-hc.css")]
    public async Task PersistedTheme_SurvivesEnhancedNavigationWithoutLoadingAnotherTheme(string themePreference, string contrast, string systemColorScheme, string expectedThemeFile) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _fixture.Context.AddInitScriptAsync(
            $$"""
            localStorage.setItem('NTComponentsStoredThemeKey', '{{themePreference}}');
            localStorage.setItem('NTComponentsStoredContrastKey', '{{contrast}}');
            """);
        await DisableBrowserCacheAsync();
        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = Enum.Parse<ColorScheme>(systemColorScheme) });
        await _page.GotoAsync($"{_appBaseUrl}/buttons", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForActiveThemeAsync(expectedThemeFile);

        var realm = await _page.EvaluateAsync<string>("window.__ntThemeTestRealm = crypto.randomUUID()");
        var navigationEntryCount = await _page.EvaluateAsync<int>("performance.getEntriesByType('navigation').length");

        await AssertEnhancedNavigationAsync("Accordion", "**/accordion", expectedThemeFile, realm, navigationEntryCount);
        await AssertEnhancedNavigationAsync("Buttons", "**/buttons", expectedThemeFile, realm, navigationEntryCount);
    }

    [Fact]
    public async Task InitialSsrAndHardRefresh_LoadTheConfiguredFallbackTheme() {
        ArgumentNullException.ThrowIfNull(_page);

        await DisableBrowserCacheAsync();
        await _page.GotoAsync($"{_appBaseUrl}/buttons", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForActiveThemeAsync("light.css");

        _requests.Should().Contain(url => url.EndsWith("/Themes/light.css", StringComparison.OrdinalIgnoreCase));
        (await _page.Locator("#nt-theme-critical").CountAsync()).Should().Be(1);
        (await _page.Locator("#nt-theme-critical[data-nt-theme-critical], #nt-theme-critical[data-tnt-theme-critical]").CountAsync()).Should().Be(0);

        ClearRequests();
        await _page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForActiveThemeAsync("light.css");

        _requests.Should().Contain(url => url.EndsWith("/Themes/light.css", StringComparison.OrdinalIgnoreCase));
        (await _page.EvaluateAsync<string>("performance.getEntriesByType('navigation')[0].type")).Should().Be("reload");
        (await _page.Locator("link[data-nt-theme],link[data-tnt-theme]").CountAsync()).Should().Be(1);
    }

    private async Task AssertEnhancedNavigationAsync(string linkName, string expectedUrl, string expectedThemeFile, string realm, int navigationEntryCount) {
        ArgumentNullException.ThrowIfNull(_page);

        ClearRequests();
        await StartNavigationProbeAsync();

        await _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName, Exact = true }).ClickAsync();
        await _page.WaitForURLAsync(expectedUrl);
        await WaitForActiveThemeAsync(expectedThemeFile);

        var probe = await StopNavigationProbeAsync();
        var themeRequests = _requests.Where(url => url.Contains("/Themes/", StringComparison.OrdinalIgnoreCase)).ToArray();

        (await _page.EvaluateAsync<string>("window.__ntThemeTestRealm")).Should().Be(realm, "enhanced navigation must retain the current JavaScript realm");
        (await _page.EvaluateAsync<int>("performance.getEntriesByType('navigation').length")).Should().Be(navigationEntryCount);
        themeRequests.Should().BeEmpty("the permanent active theme must survive without starting any theme stylesheet request");
        probe.ActiveThemeCount.Should().Be(1);
        probe.ActiveThemeHref.Should().EndWith($"/Themes/{expectedThemeFile}");
        probe.DefaultThemeCount.Should().Be(0);
        probe.CriticalThemeCount.Should().Be(0);
        probe.InsertedFallbackElements.Should().BeEmpty();
        probe.Backgrounds.Should().NotContain(color => color == "rgb(255, 255, 255)" || color == "rgb(251, 248, 255)");
    }

    private void ClearRequests() {
        while (_requests.TryDequeue(out _)) {
        }
    }

    private async Task DisableBrowserCacheAsync() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        var cdpSession = await _fixture.Context.NewCDPSessionAsync(_page);
        await cdpSession.SendAsync("Network.enable");
        await cdpSession.SendAsync("Network.setCacheDisabled", new Dictionary<string, object> { ["cacheDisabled"] = true });
    }

    private async Task StartNavigationProbeAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync(
            """
            () => {
                const probe = {
                    backgrounds: [],
                    insertedFallbackElements: []
                };
                const sampleBackgrounds = () => {
                    probe.backgrounds.push(getComputedStyle(document.documentElement).backgroundColor);
                    probe.backgrounds.push(getComputedStyle(document.body).backgroundColor);
                };
                const inspectElement = element => {
                    if (!(element instanceof Element)) {
                        return;
                    }

                    const candidates = [element, ...element.querySelectorAll('*')];
                    for (const candidate of candidates) {
                        if (candidate.matches('link[data-nt-theme-default],link[data-tnt-theme-default]')) {
                            probe.insertedFallbackElements.push('default');
                        }
                        if (candidate.matches('style[data-nt-theme-critical],style[data-tnt-theme-critical]')) {
                            probe.insertedFallbackElements.push('critical');
                        }
                    }
                };

                probe.observer = new MutationObserver(records => {
                    sampleBackgrounds();
                    for (const record of records) {
                        for (const node of record.addedNodes) {
                            inspectElement(node);
                        }
                    }
                });
                probe.observer.observe(document.head, { childList: true, subtree: true });
                const sampleFrame = () => {
                    sampleBackgrounds();
                    probe.frame = requestAnimationFrame(sampleFrame);
                };
                sampleFrame();
                window.__ntThemeNavigationProbe = probe;
            }
            """);
    }

    private async Task<NavigationProbeResult> StopNavigationProbeAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        var result = await _page.EvaluateAsync<JsonElement>(
            """
            () => {
                const probe = window.__ntThemeNavigationProbe;
                probe.observer.disconnect();
                cancelAnimationFrame(probe.frame);

                const activeThemes = [...document.head.querySelectorAll('link[data-nt-theme],link[data-tnt-theme]')];
                return {
                    activeThemeCount: activeThemes.length,
                    activeThemeHref: activeThemes[0]?.href ?? '',
                    defaultThemeCount: document.head.querySelectorAll('link[data-nt-theme-default],link[data-tnt-theme-default]').length,
                    criticalThemeCount: document.head.querySelectorAll('style[data-nt-theme-critical],style[data-tnt-theme-critical]').length,
                    insertedFallbackElements: probe.insertedFallbackElements,
                    backgrounds: probe.backgrounds
                };
            }
            """);

        return new(
            result.GetProperty("activeThemeCount").GetInt32(),
            result.GetProperty("activeThemeHref").GetString()!,
            result.GetProperty("defaultThemeCount").GetInt32(),
            result.GetProperty("criticalThemeCount").GetInt32(),
            result.GetProperty("insertedFallbackElements").EnumerateArray().Select(value => value.GetString()!).ToArray(),
            result.GetProperty("backgrounds").EnumerateArray().Select(value => value.GetString()!).ToArray());
    }

    private async Task WaitForActiveThemeAsync(string expectedThemeFile) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            """
            expectedThemeFile => {
                const activeThemes = [...document.head.querySelectorAll('link[data-nt-theme],link[data-tnt-theme]')];
                return activeThemes.length === 1
                    && new URL(activeThemes[0].href).pathname.endsWith(`/Themes/${expectedThemeFile}`)
                    && document.head.querySelectorAll('link[data-nt-theme-default],link[data-tnt-theme-default]').length === 0
                    && document.head.querySelectorAll('style[data-nt-theme-critical],style[data-tnt-theme-critical]').length === 0;
            }
            """,
            expectedThemeFile,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private sealed record NavigationProbeResult(int ActiveThemeCount, string ActiveThemeHref, int DefaultThemeCount, int CriticalThemeCount, string[] InsertedFallbackElements, string[] Backgrounds);
}
