using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Grid;

/// <summary>
///     Browser-level coverage for NTDataGrid virtualization scroll behavior.
/// </summary>
public class NTDataGridVirtualized_E2E_Tests : IAsyncLifetime {
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
    public async Task Virtualized_Grid_Renders_New_Rows_After_Scrolling() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/virtualizedGrid", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var scrollContainer = _page.Locator(".nt-data-grid-scroll").First;
        await scrollContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

        var skeletonRows = _page.Locator(".nt-data-grid-row-placeholder .nt-skeleton");
        await skeletonRows.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        (await skeletonRows.CountAsync()).Should().BeGreaterThan(0);

        await _page.Locator("input[type='number']").FillAsync("0");
        await _page.WaitForFunctionAsync(HasCustomerRowScript, 1, new PageWaitForFunctionOptions { Timeout = 10000 });

        var initialRows = await GetRenderedCustomerNumbersAsync(_page);
        initialRows.Should().Contain(1);

        var initialMetrics = await scrollContainer.EvaluateAsync<ScrollMetrics>(
            """
            element => ({
                clientHeight: element.clientHeight,
                scrollHeight: element.scrollHeight,
                scrollTop: element.scrollTop
            })
            """);
        initialMetrics.ScrollHeight.Should().BeGreaterThan(initialMetrics.ClientHeight);

        await scrollContainer.HoverAsync();
        await _page.Mouse.WheelAsync(0, 5600);
        await _page.WaitForFunctionAsync(HasCustomerRowScript, 150, new PageWaitForFunctionOptions { Timeout = 10000 });

        var scrolledRows = await GetRenderedCustomerNumbersAsync(_page);
        var scrolledMetrics = await scrollContainer.EvaluateAsync<ScrollMetrics>(
            """
            element => ({
                clientHeight: element.clientHeight,
                scrollHeight: element.scrollHeight,
                scrollTop: element.scrollTop
            })
            """);
        scrolledRows.Should().Contain(row => row >= 150);
        scrolledRows.Should().NotContain(1);
        scrolledMetrics.ScrollTop.Should().BeGreaterThan(0);
    }

    private const string HasCustomerRowScript =
        """
        minimumCustomerNumber => Array.from(document.querySelectorAll('.nt-data-grid-row .nt-data-grid-cell-content'))
            .map(element => /Customer\s+(\d+)/.exec(element.textContent ?? '')?.[1])
            .filter(value => value !== undefined)
            .map(value => Number.parseInt(value, 10))
            .some(value => value >= minimumCustomerNumber)
        """;

    private static Task<int[]> GetRenderedCustomerNumbersAsync(IPage page) =>
        page.EvaluateAsync<int[]>(
            """
            () => Array.from(document.querySelectorAll('.nt-data-grid-row .nt-data-grid-cell-content'))
                .map(element => /Customer\s+(\d+)/.exec(element.textContent ?? '')?.[1])
                .filter(value => value !== undefined)
                .map(value => Number.parseInt(value, 10))
            """);

    private sealed class ScrollMetrics {
        public int ClientHeight { get; set; }
        public int ScrollHeight { get; set; }
        public int ScrollTop { get; set; }
    }
}
