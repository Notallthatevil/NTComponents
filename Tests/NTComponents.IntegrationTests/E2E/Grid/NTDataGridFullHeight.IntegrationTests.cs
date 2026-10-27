using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Grid;

/// <summary>
///     Browser-level coverage for an NTDataGrid that fills a height-bounded flex parent.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridFullHeight_IntegrationTests : IAsyncLifetime {
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
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task FullHeight_Virtualized_Grid_Fills_Parent_And_Shows_Rows_Past_Default_Cap() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 1200);
        await _page.GotoAsync($"{AppBaseUrl}/fullHeightGrid", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var scrollContainer = _page.Locator(".nt-data-grid-scroll").First;
        await scrollContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

        // The default cap shows 20 rows of 36px, so row 24 can only sit inside the scroll viewport once the cap is gone.
        await _page.WaitForFunctionAsync(
            """
            () => {
                const scroll = document.querySelector('.nt-data-grid-scroll').getBoundingClientRect();
                return Array.from(document.querySelectorAll('.nt-data-grid-row .nt-data-grid-cell-content'))
                    .some(element => element.textContent?.trim() === 'Customer 00024' && element.getBoundingClientRect().bottom <= scroll.bottom);
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 10000 });

        var layout = await _page.EvaluateAsync<FullHeightLayout>(
            """
            () => {
                const host = document.getElementById('full-height-host');
                const grid = host.querySelector('.nt-data-grid');
                const scroll = grid.querySelector('.nt-data-grid-scroll');
                return {
                    parentContentBottom: host.getBoundingClientRect().bottom - Number.parseFloat(getComputedStyle(host).paddingBottom),
                    gridBottom: grid.getBoundingClientRect().bottom,
                    scrollClientHeight: scroll.clientHeight,
                    scrollHeight: scroll.scrollHeight
                };
            }
            """);

        layout.GridBottom.Should().BeApproximately(layout.ParentContentBottom, 1);
        layout.ScrollClientHeight.Should().BeGreaterThan(720);
        layout.ScrollHeight.Should().BeGreaterThan(layout.ScrollClientHeight);
    }

    [Fact]
    public async Task FullHeight_Virtualized_Grid_In_Unbounded_Parent_Stops_At_Viewport_Height() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 800);
        await _page.GotoAsync($"{AppBaseUrl}/fullHeightGrid", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var grid = _page.Locator("#unbounded-host .nt-data-grid");
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });
        await _page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#unbounded-host .nt-data-grid-row .nt-data-grid-cell-content').length > 0",
            null,
            new PageWaitForFunctionOptions { Timeout = 10000 });

        var gridHeight = await grid.EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        var renderedRows = await grid.EvaluateAsync<int>("element => element.querySelectorAll('tbody .nt-data-grid-row:not(.nt-data-grid-row-placeholder)').length");

        gridHeight.Should().BeLessThanOrEqualTo(800);
        renderedRows.Should().BeLessThan(100);
    }

    private sealed class FullHeightLayout {
        public double ParentContentBottom { get; set; }
        public double GridBottom { get; set; }
        public double ScrollClientHeight { get; set; }
        public double ScrollHeight { get; set; }
    }
}
