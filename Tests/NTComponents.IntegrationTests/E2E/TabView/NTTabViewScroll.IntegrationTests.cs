using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.TabView;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTTabViewScroll_IntegrationTests : IAsyncLifetime {
    private const string PagePath = "/nt-tab-view-initial-scroll-test";
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        // Instant scrolling makes any scrollIntoView call observable without waiting on smooth-scroll animation.
        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("", "Section 1")]
    [InlineData("?section=section-8", "Section 8")]
    public async Task InitialLoad_BelowTheFold_LeavesPageAtTop(string query, string expectedTab) {
        var tabView = await LoadAsync(query);

        (await GetTab(tabView, expectedTab).GetAttributeAsync("aria-selected")).Should().Be("true");
        (await tabView.EvaluateAsync<double>(
            """
            tabView => {
                let scrollTop = window.scrollY;
                for (let element = tabView.parentElement; element; element = element.parentElement) {
                    scrollTop += element.scrollTop;
                }
                return scrollTop;
            }
            """)).Should().Be(0);
    }

    [Theory]
    [InlineData("click")]
    [InlineData("ArrowRight")]
    public async Task UserNavigation_ScrollsTargetTabIntoOverflowingTabStrip(string action) {
        var tabView = await LoadAsync(string.Empty);
        var lastTab = GetTab(tabView, "Section 8");
        var tabList = tabView.Locator(".nt-tab-view-tablist");
        (await tabList.EvaluateAsync<bool>("list => list.scrollWidth > list.clientWidth && list.scrollLeft === 0")).Should().BeTrue();

        if (action == "click") {
            // Dispatch directly so Playwright's own actionability scrolling cannot mask the component behavior.
            await lastTab.DispatchEventAsync("click");
        }
        else {
            await GetTab(tabView, "Section 7").EvaluateAsync("tab => tab.focus({ preventScroll: true })");
            await _page!.Keyboard.PressAsync(action);
            (await lastTab.EvaluateAsync<bool>("tab => tab === document.activeElement")).Should().BeTrue();
        }

        await WaitForFramesAsync();
        (await tabList.EvaluateAsync<bool>(
            """
            list => {
                const listRect = list.getBoundingClientRect();
                const tabRect = list.querySelector('[data-nt-tab-value="section-8"]').getBoundingClientRect();
                return list.scrollLeft > 0 && tabRect.left >= listRect.left - 1 && tabRect.right <= listRect.right + 1;
            }
            """)).Should().BeTrue();
    }

    private async Task<ILocator> LoadAsync(string query) {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}{PagePath}{query}");
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
        var tabView = _page.GetByTestId("below-fold-tab-view");
        await tabView.EvaluateAsync("tabView => new Promise(resolve => { const check = () => tabView.__ntTabViewState ? resolve() : requestAnimationFrame(check); check(); })");
        await WaitForFramesAsync();
        return tabView;
    }

    private Task WaitForFramesAsync() => _page!.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private static ILocator GetTab(ILocator tabView, string name) => tabView.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = name, Exact = true });
}
