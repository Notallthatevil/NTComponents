using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Grid;

/// <summary>
///     Browser-level coverage that virtualized placeholder rows occupy exactly the space of the data rows they stand in for.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridPlaceholderRows_IntegrationTests : IAsyncLifetime {
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

    [Theory]
    [InlineData("compact", 28)]
    [InlineData("standard", 36)]
    public async Task Placeholder_Rows_Match_Data_Row_Height(string density, double rowHeight) {
        ArgumentNullException.ThrowIfNull(_page);

        await OpenAsync(_page, density, details: false);
        await WaitForRowsAsync(_page, "tbody tr.nt-data-grid-row-placeholder");

        var heights = await GetRowHeightsAsync(_page);
        heights.Data.Should().NotBeEmpty().And.AllSatisfy(height => height.Should().BeApproximately(rowHeight, 0.01));
        heights.Placeholder.Should().NotBeEmpty().And.AllSatisfy(height => height.Should().BeApproximately(rowHeight, 0.01));
    }

    [Theory]
    [InlineData("compact")]
    [InlineData("standard")]
    public async Task Placeholder_Rows_Match_Data_Row_Height_With_Row_Details_And_Custom_Item_Size(string density) {
        ArgumentNullException.ThrowIfNull(_page);

        // The expand button makes detail rows taller than the density row, so size placeholders from a measured data row.
        await OpenAsync(_page, density, details: true);
        var dataRowHeight = GetTypicalHeight((await GetRowHeightsAsync(_page)).Data);

        // Detail grids render placeholders only for unloaded rows inside the visible range, so scroll past the loaded window.
        await OpenAsync(_page, density, details: true, itemSize: dataRowHeight);
        await _page.Locator(".nt-data-grid-scroll").EvaluateAsync("element => element.scrollTop = 4000");
        await WaitForRowsAsync(_page, "tbody tr.nt-data-grid-row-placeholder");

        dataRowHeight.Should().BeGreaterThan(0);
        (await GetRowHeightsAsync(_page)).Placeholder.Should().NotBeEmpty().And.AllSatisfy(height => height.Should().BeApproximately(dataRowHeight, 0.01));
    }

    // The first row can sit on a half pixel below the top spacer, so use the height most rows share.
    private static double GetTypicalHeight(double[] heights) => heights.GroupBy(height => height).MaxBy(group => group.Count())?.Key ?? 0;

    private async Task OpenAsync(IPage page, string density, bool details, double? itemSize = null) {
        var itemSizeQuery = itemSize is { } size ? $"&itemSize={size.ToString(System.Globalization.CultureInfo.InvariantCulture)}" : string.Empty;
        await page.GotoAsync($"{AppBaseUrl}/gridPlaceholderRows?density={density}&details={(details ? "true" : "false")}{itemSizeQuery}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForRowsAsync(page, "tbody tr.nt-data-grid-row:not(.nt-data-grid-row-placeholder) .nt-data-grid-cell-content");
    }

    private static Task WaitForRowsAsync(IPage page, string selector) =>
        page.WaitForFunctionAsync("selector => document.querySelector(selector) !== null", selector, new PageWaitForFunctionOptions { Timeout = 10000 });

    private static Task<RowHeights> GetRowHeightsAsync(IPage page) =>
        page.EvaluateAsync<RowHeights>(
            """
            () => {
                const heightsOf = selector => Array.from(document.querySelectorAll(selector)).map(row => row.getBoundingClientRect().height);
                return {
                    data: heightsOf('tbody tr.nt-data-grid-row:not(.nt-data-grid-row-placeholder)'),
                    placeholder: heightsOf('tbody tr.nt-data-grid-row-placeholder')
                };
            }
            """);

    private sealed class RowHeights {
        public double[] Data { get; set; } = [];
        public double[] Placeholder { get; set; } = [];
    }
}
