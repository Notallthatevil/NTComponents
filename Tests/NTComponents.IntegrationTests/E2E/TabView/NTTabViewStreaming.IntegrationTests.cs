using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.TabView;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTTabViewStreaming_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task StreamedInteractiveAutoRender_InitiallyContainsAccessibleTabsAndSelectedPanel() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _fixture.Context.AddInitScriptAsync(
            """
            window.__ntTabViewInitial = null;

            const captureInitialTabView = () => {
                if (window.__ntTabViewInitial !== null) {
                    return;
                }

                const tabView = document.querySelector('[data-testid="streamed-tab-view"]');
                const header = tabView?.querySelector('.nt-tab-view-header');
                const headerStyle = header instanceof HTMLElement ? getComputedStyle(header) : null;
                if (headerStyle?.borderBottomWidth !== '1px' || headerStyle.visibility !== 'visible') {
                    requestAnimationFrame(captureInitialTabView);
                    return;
                }

                const selectedTab = header.querySelector('[role="tab"][aria-selected="true"] .nt-tab-view-label');
                const selectedPanel = tabView.querySelector('[role="tabpanel"]:not([hidden])');
                window.__ntTabViewInitial = {
                    tabCount: header.querySelectorAll('[role="tab"]').length,
                    headerHeight: header.getBoundingClientRect().height,
                    selectedTab: selectedTab?.textContent?.trim() ?? null,
                    selectedPanelText: selectedPanel?.textContent?.trim() ?? null,
                    streamingComplete: document.querySelector('[data-testid="streaming-complete"]') !== null,
                    interactive: document.querySelector('[data-testid="interactive-renderer"]') !== null
                };
            };

            requestAnimationFrame(captureInitialTabView);
            """);

        await _page.GotoAsync($"{_fixture.ServerAddress}/nt-tab-view-streaming-test", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForFunctionAsync("window.__ntTabViewInitial !== null", null, new PageWaitForFunctionOptions { Timeout = 5000 });
        var initialTabCount = await _page.EvaluateAsync<int>("window.__ntTabViewInitial.tabCount");
        var initialHeaderHeight = await _page.EvaluateAsync<double>("window.__ntTabViewInitial.headerHeight");

        initialTabCount.Should().Be(2, "the first streamed header was {0}px high", initialHeaderHeight);
        initialHeaderHeight.Should().BeApproximately(37, 0.5);
        (await _page.EvaluateAsync<string?>("window.__ntTabViewInitial.selectedTab")).Should().Be("Diary Notes");
        (await _page.EvaluateAsync<string?>("window.__ntTabViewInitial.selectedPanelText")).Should().Be("Diary notes");
        (await _page.EvaluateAsync<bool>("window.__ntTabViewInitial.streamingComplete")).Should().BeFalse();
        (await _page.EvaluateAsync<bool>("window.__ntTabViewInitial.interactive")).Should().BeFalse();

        await _page.GetByTestId("streaming-complete").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
        var tabView = _page.GetByTestId("streamed-tab-view");
        (await tabView.GetByRole(AriaRole.Tab).CountAsync()).Should().Be(2);
        (await tabView.Locator(".nt-tab-view-header").EvaluateAsync<double>("header => header.getBoundingClientRect().height")).Should().BeApproximately(37, 0.5);
        await AssertSelectedPanelAsync(tabView, "Diary Notes", "Summary");
        (await _page.GetByTestId("interactive-renderer").TextContentAsync()).Should().Be("Server");
        await SelectSummaryAndAssertAsync(tabView);
    }

    [Theory]
    [InlineData("/nt-tab-view-streaming-server-test", "Server")]
    [InlineData("/nt-tab-view-streaming-webassembly-test", "WebAssembly")]
    public async Task StreamedInteractiveRender_SettlesWithAccessibleTabsAndSelectedPanel(string path, string expectedRenderer) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });

        var tabView = _page.GetByTestId("streamed-tab-view");
        (await tabView.GetByRole(AriaRole.Tab).CountAsync()).Should().Be(2);
        (await tabView.Locator(".nt-tab-view-header").EvaluateAsync<double>("header => header.getBoundingClientRect().height")).Should().BeApproximately(37, 0.5);
        await AssertSelectedPanelAsync(tabView, "Diary Notes", "Summary");
        (await _page.GetByTestId("interactive-renderer").TextContentAsync()).Should().Be(expectedRenderer);
        await SelectSummaryAndAssertAsync(tabView);
    }

    [Theory]
    [InlineData("/nt-tab-view-streaming-test")]
    [InlineData("/nt-tab-view-streaming-server-test")]
    [InlineData("/nt-tab-view-streaming-webassembly-test")]
    public async Task StreamedInteractiveRender_SupportsKeyboardFocusAndActivationAfterHydration(string path) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });

        var tabView = _page.GetByTestId("streamed-tab-view");
        var summaryTab = GetTab(tabView, "Summary");
        var diaryNotesTab = GetTab(tabView, "Diary Notes");
        await diaryNotesTab.FocusAsync();
        await diaryNotesTab.PressAsync("ArrowLeft");

        (await summaryTab.EvaluateAsync<bool>("tab => tab === document.activeElement")).Should().BeTrue();
        await AssertSelectedPanelAsync(tabView, "Diary Notes", "Summary");

        await summaryTab.PressAsync("Enter");

        await AssertSelectedPanelAsync(tabView, "Summary", "Diary Notes");
    }

    [Theory]
    [InlineData("/nt-tab-view-streaming-test")]
    [InlineData("/nt-tab-view-streaming-server-test")]
    [InlineData("/nt-tab-view-streaming-webassembly-test")]
    public async Task StreamedPrerender_ContainsAccessibleTabsWithoutBlazorJavaScript(string path) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _fixture.Context.RouteAsync("**/_framework/blazor.web.js", route => route.AbortAsync());
        await _page.GotoAsync($"{_fixture.ServerAddress}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var tabView = _page.GetByTestId("streamed-tab-view");
        (await tabView.GetByRole(AriaRole.Tab).CountAsync()).Should().Be(2);
        (await tabView.Locator(".nt-tab-view-header").EvaluateAsync<double>("header => header.getBoundingClientRect().height")).Should().BeApproximately(37, 0.5);
        await AssertSelectedPanelAsync(tabView, "Diary Notes", "Summary");
    }

    private static async Task SelectSummaryAndAssertAsync(ILocator tabView) {
        var summaryTab = GetTab(tabView, "Summary");
        await summaryTab.ClickAsync();

        await AssertSelectedPanelAsync(tabView, "Summary", "Diary Notes");
    }

    private static async Task AssertSelectedPanelAsync(ILocator tabView, string selectedName, string unselectedName) {
        var selectedTab = GetTab(tabView, selectedName);
        var unselectedTab = GetTab(tabView, unselectedName);
        var selectedPanel = tabView.GetByRole(AriaRole.Tabpanel, new LocatorGetByRoleOptions { Name = selectedName, Exact = true });
        var unselectedPanel = tabView.GetByRole(AriaRole.Tabpanel, new LocatorGetByRoleOptions { Name = unselectedName, Exact = true });

        (await selectedTab.GetAttributeAsync("aria-selected")).Should().Be("true");
        (await unselectedTab.GetAttributeAsync("aria-selected")).Should().Be("false");
        (await selectedPanel.IsVisibleAsync()).Should().BeTrue();
        (await unselectedPanel.IsVisibleAsync()).Should().BeFalse();
    }

    private static ILocator GetTab(ILocator tabView, string name) => tabView.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = name, Exact = true });
}
