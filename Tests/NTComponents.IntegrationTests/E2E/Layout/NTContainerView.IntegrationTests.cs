using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Layout;

/// <summary>
///     Browser-level coverage for container view reflow and sticky quick-navigation placement.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTContainerView_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    // Behavior source: NTContainerView documentation says it controls horizontal placement and width, and the
    // user-requested regression contract requires its desktop composition to reflow when its actual host is narrow.
    [Fact]
    public async Task Views_Container_Stacks_QuickNav_When_Host_Is_Narrow_At_Desktop_Viewport() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        await _page.EvaluateAsync(
            """
            () => {
                const view = document.querySelector('.nt-container-view');
                const host = view?.closest('.demo-section');
                if (!(host instanceof HTMLElement)) {
                    throw new Error('Unable to find the container-view demo host.');
                }

                host.style.inlineSize = '380px';
            }
            """);

        var quickNavIsStacked = await _page.EvaluateAsync<bool>(
            """
            () => {
                const view = document.querySelector('.nt-container-view');
                const quickNav = view?.querySelector('.nt-container-view-quick-nav');
                const content = view?.querySelector('.nt-container-view-content');
                if (!(view instanceof HTMLElement) || !(quickNav instanceof HTMLElement) || !(content instanceof HTMLElement)) {
                    return false;
                }

                const quickNavRect = quickNav.getBoundingClientRect();
                const contentRect = content.getBoundingClientRect();
                return view.getBoundingClientRect().width < 400
                    && quickNavRect.bottom <= contentRect.top;
            }
            """);

        quickNavIsStacked.Should().BeTrue(
            "the quick navigation must leave the desktop side column when the container itself is not expanded");
    }

    // Behavior source: the user-requested sticky-offset contract requires quick navigation to clear a fixed
    // layout header; NTHeader defaults to a fixed 64px region and the view spacing token is 16px.
    [Fact]
    public async Task Views_Container_QuickNav_Sticky_Offset_Clears_Fixed_Header() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        var stickyOffsetClearsHeader = await _page.EvaluateAsync<bool>(
            """
            () => {
                const header = document.querySelector('.nt-layout > .nt-header-fixed-position');
                const view = document.querySelector('.nt-container-view');
                const quickNav = view?.querySelector('.nt-container-view-quick-nav');
                if (!(header instanceof HTMLElement) || !(view instanceof HTMLElement) || !(quickNav instanceof HTMLElement)) {
                    return false;
                }

                const headerHeight = header.getBoundingClientRect().height;
                view.style.setProperty('--tnt-header-height', `${headerHeight}px`);
                const stickyOffset = Number.parseFloat(getComputedStyle(quickNav).top);
                return getComputedStyle(quickNav).position === 'sticky'
                    && stickyOffset >= headerHeight + 16;
            }
            """);

        stickyOffsetClearsHeader.Should().BeTrue(
            "the sticky quick navigation must honor the fixed-header height plus its 16px layout gap");
    }

    private async Task NavigateToViewsAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/views", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-container-view-with-quick-nav .nt-container-view-quick-nav").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }
}
