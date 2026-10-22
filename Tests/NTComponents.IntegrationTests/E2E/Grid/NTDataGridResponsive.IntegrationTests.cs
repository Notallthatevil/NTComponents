using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridResponsive_IntegrationTests : IAsyncLifetime {
    private readonly PlaywrightFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Theory]
    [InlineData("Small and up", "small Order 1", 600)]
    [InlineData("Medium and up", "medium Order 1", 840)]
    [InlineData("Large and up", "large Order 1", 1200)]
    [InlineData("Extra large and up", "extra large Order 1", 1600)]
    public async Task BelowColumnBreakpoint_HidesColumnWithoutReservingItsWidth(string header, string cell, int breakpoint) {
        var page = await OpenStaticGridAsync(breakpoint - 1);

        // IncludeHidden keeps the locators bound to the real elements, so a misspelled name cannot pass as hidden.
        var columnHeader = page.GetByRole(AriaRole.Columnheader, new() { Name = header, Exact = true, IncludeHidden = true });
        var firstCell = page.GetByRole(AriaRole.Cell, new() { Name = cell, Exact = true, IncludeHidden = true });
        await Expect(columnHeader).ToHaveCountAsync(1);
        await Expect(columnHeader).ToBeHiddenAsync();
        await Expect(firstCell).ToBeHiddenAsync();
        await ExpectVisibleHeadersToSpanTableAsync(page);
    }

    [Theory]
    [InlineData("Small and up", "small Order 1", 600)]
    [InlineData("Medium and up", "medium Order 1", 840)]
    [InlineData("Large and up", "large Order 1", 1200)]
    [InlineData("Extra large and up", "extra large Order 1", 1600)]
    public async Task AtColumnBreakpoint_ShowsColumn(string header, string cell, int breakpoint) {
        var page = await OpenStaticGridAsync(breakpoint);

        await Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = header, Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = cell, Exact = true })).ToBeVisibleAsync();
        await ExpectVisibleHeadersToSpanTableAsync(page);
    }

    [Fact]
    public async Task BelowLargeBreakpoint_PaginationShowsPreviousPageDropdownAndNext() {
        var page = await OpenStaticGridAsync(1199);

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Previous page", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Go to page" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true })).ToBeVisibleAsync();
        foreach (var hiddenLink in new[] { "First page", "Page 2", "Last page" }) {
            var link = page.GetByRole(AriaRole.Link, new() { Name = hiddenLink, Exact = true, IncludeHidden = true });
            await Expect(link).ToHaveCountAsync(1);
            await Expect(link).ToBeHiddenAsync();
        }
    }

    [Fact]
    public async Task AtLargeBreakpoint_PaginationShowsNumberedPagesInsteadOfDropdown() {
        var page = await OpenStaticGridAsync(1200);

        foreach (var visibleLink in new[] { "First page", "Previous page", "Page 2", "Next page", "Last page" }) {
            await Expect(page.GetByRole(AriaRole.Link, new() { Name = visibleLink, Exact = true })).ToBeVisibleAsync();
        }

        var pageSelect = page.GetByRole(AriaRole.Combobox, new() { Name = "Go to page", IncludeHidden = true });
        await Expect(pageSelect).ToHaveCountAsync(1);
        await Expect(pageSelect).ToBeHiddenAsync();
    }

    [Fact]
    public async Task StaticSsrPageDropdown_NavigatesToSelectedPage() {
        var page = await OpenStaticGridAsync(390);
        var pageSelect = page.GetByRole(AriaRole.Combobox, new() { Name = "Go to page" });
        // The inline handler is a no-op until NTComponents.lib.module.js has defined it, so wait for it before choosing.
        await page.WaitForFunctionAsync("() => typeof window.NTComponents?.jumpToGridPage === 'function'");

        await pageSelect.SelectOptionAsync("3");

        await Expect(page).ToHaveURLAsync(new Regex("static-grid-page=3"));
        await Expect(page.GetByRole(AriaRole.Cell, new() { Name = "Order 11", Exact = true })).ToBeVisibleAsync();
        // Enhanced navigation patches in the new page's select, which must still carry the inline handler.
        await Expect(pageSelect).ToHaveValueAsync("3");
        await Expect(pageSelect).ToHaveAttributeAsync("onchange", "window.NTComponents?.jumpToGridPage(this)");
    }

    [Fact]
    public async Task WithoutTouch_PaginationButtonsKeepTheirDefaultSize() {
        var page = await OpenStaticGridAsync(390);

        var nextPage = await page.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true }).BoundingBoxAsync();

        nextPage.Should().NotBeNull();
        nextPage!.Height.Should().BeLessThan(48, "the 48px touch target applies only to coarse pointers");
    }

    private async Task<IPage> OpenStaticGridAsync(int width) {
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(width, 900);
        await page.GotoAsync($"{_fixture.ServerAddress}/static-paginated-grid");
        return page;
    }

    // A <col> left in the layout keeps its 120px track, so the visible headers would fall short of the table width.
    private static async Task ExpectVisibleHeadersToSpanTableAsync(IPage page) {
        var widths = await page.EvaluateAsync<double[]>(@"() => {
            const table = document.querySelector('table');
            const visibleHeaders = [...table.querySelectorAll('thead th')].filter(th => th.getClientRects().length > 0);
            return [table.getBoundingClientRect().width, visibleHeaders.reduce((sum, th) => sum + th.getBoundingClientRect().width, 0)];
        }");

        widths[1].Should().BeApproximately(widths[0], 1);
    }
}
