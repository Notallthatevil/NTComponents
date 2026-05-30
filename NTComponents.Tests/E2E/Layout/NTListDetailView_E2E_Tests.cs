using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Layout;

/// <summary>
///     Browser-level coverage for list-detail view SSR markup, CSS isolation, and page-script enhancement.
/// </summary>
public class NTListDetailView_E2E_Tests : IAsyncLifetime {
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

    [Fact]
    public async Task Views_ListDetail_Constrains_Pane_Content_To_Grid_Bounds() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        await _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Release notes Design systems Draft" }).ClickAsync();
        await _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Migration plan Platform Ready for review" }).ClickAsync();

        var overflowModes = await _page.EvaluateAsync<string[]>(
            """
            () => [
                getComputedStyle(document.querySelector('.nt-list-detail-view-list')).overflowX,
                getComputedStyle(document.querySelector('.nt-list-detail-view-detail')).overflowX
            ]
            """);

        overflowModes.Should().OnlyContain(mode => mode == "clip" || mode == "hidden",
            "pane content should not be able to paint outside the owning grid cell");

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
            "interactive list content should have enough internal inset to avoid being clipped by pane containment");
    }

    private async Task NavigateToViewsAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/views", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-list-detail-view").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }
}
