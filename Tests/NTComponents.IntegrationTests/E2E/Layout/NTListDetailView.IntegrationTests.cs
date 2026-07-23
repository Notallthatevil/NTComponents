using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Layout;

/// <summary>
///     Browser-level coverage for list-detail view SSR markup, CSS isolation, and page-script enhancement.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTListDetailView_IntegrationTests : IAsyncLifetime {
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

    [Fact]
    public async Task Views_ListDetail_Shows_Only_Selected_Detail_Panel() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToViewsAsync();

        var panels = _page.Locator(".nt-list-detail-view [data-nt-list-detail-panel]");
        (await panels.CountAsync()).Should().Be(3);

        (await panels.Filter(new LocatorFilterOptions { HasTextString = "Migration plan" }).IsVisibleAsync()).Should().BeTrue();
        (await panels.Filter(new LocatorFilterOptions { HasTextString = "Release notes" }).IsVisibleAsync()).Should().BeFalse();
        (await panels.Filter(new LocatorFilterOptions { HasTextString = "Accessibility audit" }).IsVisibleAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Views_ListDetail_Does_Not_Overlap_List_And_Detail_Panes_In_TwoPane_Width() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        var panesOverlap = await _page.EvaluateAsync<bool>(
            """
            () => {
                const list = document.querySelector('.nt-list-detail-view-list');
                const detail = document.querySelector('.nt-list-detail-view-detail');
                if (!list || !detail) {
                    return true;
                }

                const listRect = list.getBoundingClientRect();
                const detailRect = detail.getBoundingClientRect();

                return listRect.left < detailRect.right
                    && listRect.right > detailRect.left
                    && listRect.top < detailRect.bottom
                    && listRect.bottom > detailRect.top;
            }
            """);

        panesOverlap.Should().BeFalse("list and detail panes must occupy separate grid columns at expanded widths");
    }

    // Behavior source: NTListDetailViewMode.Auto documentation says the component adapts to available width,
    // using one pane at compact/medium widths and two panes only at expanded widths.
    [Fact]
    public async Task Views_ListDetail_Auto_Uses_One_Pane_When_Host_Is_Narrow_At_Desktop_Viewport() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();
        await ConstrainViewHostAsync(".nt-list-detail-view", 380);

        var usesOnePane = await _page.EvaluateAsync<bool>(
            """
            () => {
                const view = document.querySelector('.nt-list-detail-view');
                const list = view?.querySelector('.nt-list-detail-view-list');
                const detail = view?.querySelector('.nt-list-detail-view-detail');
                if (!(view instanceof HTMLElement) || !(list instanceof HTMLElement) || !(detail instanceof HTMLElement)) {
                    return false;
                }

                const visiblePaneCount = [list, detail]
                    .filter(pane => getComputedStyle(pane).display !== 'none')
                    .length;

                return view.getBoundingClientRect().width < 400 && visiblePaneCount === 1;
            }
            """);

        usesOnePane.Should().BeTrue(
            "Auto mode must use the width actually available to the view, even when the browser viewport is expanded");
    }

    // Behavior source: the user-requested regression contract requires overflowing pane content to remain
    // scroll-reachable rather than being discarded by overflow clipping.
    [Fact]
    public async Task Views_ListDetail_Overflowing_Detail_Content_Remains_Scroll_Reachable() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        var contentIsReachable = await _page.EvaluateAsync<bool>(
            """
            () => {
                const view = document.querySelector('.nt-list-detail-view');
                const detail = view?.querySelector('.nt-list-detail-view-detail');
                const panel = detail?.querySelector('[data-nt-list-detail-panel]:not([hidden])');
                if (!(view instanceof HTMLElement) || !(detail instanceof HTMLElement) || !(panel instanceof HTMLElement)) {
                    return false;
                }

                view.style.blockSize = '240px';

                const probe = document.createElement('div');
                probe.setAttribute('data-nt-scroll-reachability-probe', 'true');
                probe.style.blockSize = '1px';
                probe.style.marginBlockStart = '700px';
                panel.append(probe);

                detail.scrollTop = detail.scrollHeight;

                const detailRect = detail.getBoundingClientRect();
                const probeRect = probe.getBoundingClientRect();
                return getComputedStyle(detail).overflowY === 'auto'
                    && detail.scrollHeight > detail.clientHeight
                    && detail.scrollTop > 0
                    && probeRect.bottom <= detailRect.bottom + 1;
            }
            """);

        contentIsReachable.Should().BeTrue(
            "users must be able to scroll to all detail content when the view is height-constrained");
    }

    // Behavior source: NTListDetailViewMode.TwoPane documentation requires both panes to have bounded content
    // that can shrink without horizontal overflow.
    [Fact]
    public async Task Views_ListDetail_Keeps_List_Items_Inside_Pane_Bounds() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        await _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Release notes Design systems Draft" }).ClickAsync();
        await _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Migration plan Platform Ready for review" }).ClickAsync();

        var listItemIsInsetWithinPane = await _page.EvaluateAsync<bool>(
            """
            () => {
                const list = document.querySelector('.nt-list-detail-view-list');
                const item = document.querySelector('.nt-list-detail-view-list .demo-list-item');
                if (!list || !item) {
                    return false;
                }

                const listRect = list.getBoundingClientRect();
                const itemRect = item.getBoundingClientRect();

                return itemRect.left > listRect.left
                    && itemRect.right < listRect.right
                    && itemRect.top > listRect.top;
            }
            """);

        listItemIsInsetWithinPane.Should().BeTrue(
            "interactive list content should remain within the owning grid cell");
    }

    private async Task ConstrainViewHostAsync(string viewSelector, int inlineSize) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync(
            """
            ([selector, width]) => {
                const view = document.querySelector(selector);
                const host = view?.closest('.demo-section');
                if (!(host instanceof HTMLElement)) {
                    throw new Error(`Unable to find a demo host for ${selector}.`);
                }

                host.style.inlineSize = `${width}px`;
            }
            """,
            new object[] { viewSelector, inlineSize });
    }

    private async Task NavigateToViewsAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/views", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-list-detail-view").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }
}
