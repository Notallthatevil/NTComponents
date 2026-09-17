using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridFeatures_IntegrationTests : IAsyncLifetime {
    private readonly PlaywrightFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    private async Task OpenAsync() {
        await _fixture.Page.SetViewportSizeAsync(1440, 1050);
        await _fixture.Page.GotoAsync($"{_fixture.ServerAddress}/virtualizationFeatures");
        await Expect(_fixture.Page.Locator(".nt-data-grid-row").Filter(new() { HasText = "Row 40" })).ToBeVisibleAsync();
        await _fixture.Page.WaitForFunctionAsync("() => document.querySelector('.nt-data-grid-scroll').scrollTop > 100");
    }

    private Task WaitForAlignedRowAsync(int id) => _fixture.Page.WaitForFunctionAsync("""
        id => {
            const scroll = document.querySelector('.nt-data-grid-scroll');
            const row = Array.from(scroll.querySelectorAll('tr.nt-data-grid-row')).find(row => row.cells[0]?.textContent.trim() === String(id));
            if (!row) return false;
            const headerBottom = Math.max(...Array.from(scroll.querySelectorAll('thead th')).map(cell => cell.getBoundingClientRect().bottom));
            return Math.abs(row.getBoundingClientRect().top - headerBottom) <= 2;
        }
        """, id);

    [Fact]
    public async Task InitialIndexAndProgrammaticScroll_PositionRequestedRows() {
        await OpenAsync();
        await WaitForAlignedRowAsync(40);
        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Go to item 100", Exact = true }).ClickAsync();
        await WaitForAlignedRowAsync(100);
        await Expect(_fixture.Page.Locator("table")).ToHaveAttributeAsync("aria-rowcount", "201");
        await Expect(_fixture.Page.Locator("tr.nt-data-grid-row").Filter(new() { HasText = "Row 100" })).ToHaveAttributeAsync("aria-rowindex", "102");
    }

    [Fact]
    public async Task PrependingFreshInstances_PreservesVisibleRowPosition() {
        await OpenAsync();
        await WaitForAlignedRowAsync(40);
        var row = _fixture.Page.Locator("tr.nt-data-grid-row").Filter(new() { HasText = "Row 42" });
        var before = await row.EvaluateAsync<double>("element => element.getBoundingClientRect().top");
        await _fixture.Page.GetByRole(AriaRole.Button, new() { Name = "Prepend item", Exact = true }).ClickAsync();
        await Expect(_fixture.Page.GetByLabel("Refresh count")).ToHaveTextAsync("1");
        await _fixture.Page.WaitForFunctionAsync("""
            before => {
                const row = Array.from(document.querySelectorAll('tr.nt-data-grid-row')).find(row => row.cells[0]?.textContent.trim() === '42');
                return row && Math.abs(row.getBoundingClientRect().top - before) <= 2;
            }
            """, before);
        await Expect(_fixture.Page.Locator("table")).ToHaveAttributeAsync("aria-rowcount", "202");
    }

    [Fact]
    public async Task EndAnchoring_FollowsAppendsOnlyAtBottom() {
        await OpenAsync();
        var page = _fixture.Page;
        await page.GetByLabel("Anchor at end").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Go to last item", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => { const e = document.querySelector('.nt-data-grid-scroll'); return Math.abs(e.scrollHeight - e.clientHeight - e.scrollTop) <= 2; }");
        await page.GetByRole(AriaRole.Button, new() { Name = "Append item", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Refresh count")).ToHaveTextAsync("1");
        await Expect(page.Locator("tr.nt-data-grid-row").Filter(new() { HasText = "Row 200" })).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => { const e = document.querySelector('.nt-data-grid-scroll'); return Math.abs(e.scrollHeight - e.clientHeight - e.scrollTop) <= 2; }");
        await page.GetByRole(AriaRole.Button, new() { Name = "Go to item 100", Exact = true }).ClickAsync();
        await WaitForAlignedRowAsync(100);
        var before = await page.Locator(".nt-data-grid-scroll").EvaluateAsync<double>("element => element.scrollTop");
        await page.GetByRole(AriaRole.Button, new() { Name = "Append item", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Refresh count")).ToHaveTextAsync("2");
        var after = await page.Locator(".nt-data-grid-scroll").EvaluateAsync<double>("element => element.scrollTop");
        after.Should().BeApproximately(before, 2);
    }

    [Fact]
    public async Task OrdinaryRowMeasurement_TracksContentAndDensityChanges() {
        await OpenAsync();
        var page = _fixture.Page;
        await page.GetByLabel("Taller ordinary rows").CheckAsync();
        await page.WaitForFunctionAsync("() => Array.from(document.querySelectorAll('tr.nt-data-grid-row[data-nt-virtualize-index]')).some(row => row.getBoundingClientRect().height > 60)");
        await page.GetByLabel("Compact rows").CheckAsync();
        await Expect(page.Locator(".nt-data-grid")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("nt-data-grid-compact"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Go to item 100", Exact = true }).ClickAsync();
        await WaitForAlignedRowAsync(100);
        await Expect(page.Locator(".nt-data-grid-detail-row")).ToHaveCountAsync(0);
    }
}
