using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridDragToScroll_IntegrationTests : IAsyncLifetime {
    private const int _dragDistance = 150;
    // Wide enough for the fixture's table to fit its grid.
    private const int _wideViewport = 1600;

    private readonly PlaywrightFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task StaticSsrDragFromSortHeader_ScrollsTheTableSideways() {
        var page = await OpenStaticGridAsync();

        await DragAsync(page, OrderHeader(page), -_dragDistance);

        (await StripScrollLeftAsync(page)).Should().BeGreaterThan(_dragDistance / 2, "the table follows the mouse");
    }

    [Fact]
    public async Task StaticSsrDragFromSortHeader_DoesNotFollowTheLink() {
        var page = await OpenStaticGridAsync();
        await DragAsync(page, OrderHeader(page), -_dragDistance);

        // One deliberate click must sort ascending. Had the drag also clicked the header, this second click would sort descending.
        await OrderHeader(page).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("static-grid-sort=Name%3Aasc"));
    }

    [Fact]
    public async Task StaticSsrPressThatBarelyMoves_StillSorts() {
        var page = await OpenStaticGridAsync();

        await DragAsync(page, OrderHeader(page), -2);

        await Expect(page).ToHaveURLAsync(new Regex("static-grid-sort=Name%3Aasc"));
    }

    [Fact]
    public async Task HoverOverTableWiderThanItsGrid_ShowsGrabCursor() {
        var page = await OpenStaticGridAsync();
        var cell = page.GetByRole(AriaRole.Cell, new() { Name = "Order 1", Exact = true });

        await cell.HoverAsync();

        await Expect(cell).ToHaveCSSAsync("cursor", "grab");
    }

    [Fact]
    public async Task WhileDragging_ShowsGrabbingCursor() {
        var page = await OpenStaticGridAsync();
        var cell = page.GetByRole(AriaRole.Cell, new() { Name = "Order 1", Exact = true });
        await cell.ScrollIntoViewIfNeededAsync();
        var box = (await cell.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();

        await page.Mouse.MoveAsync(box.X + box.Width / 2 - 40, box.Y + box.Height / 2, new() { Steps = 4 });

        await Expect(cell).ToHaveCSSAsync("cursor", "grabbing");
        await page.Mouse.UpAsync();
    }

    [Fact]
    public async Task HoverOverTableThatFits_KeepsTheDefaultCursor() {
        var page = await OpenStaticGridAsync(_wideViewport, expectOverflow: false);
        var cell = page.GetByRole(AriaRole.Cell, new() { Name = "Order 1", Exact = true });

        await cell.HoverAsync();

        await Expect(cell).ToHaveCSSAsync("cursor", "auto");
    }

    [Fact]
    public async Task TableAtItsStart_FadesOnlyTheEdgeWithMoreColumns() {
        var page = await OpenFadingGridAsync();

        await WaitForFadeAsync(page, "end", "24px");
        (await GetFadeWidthsAsync(page)).Should().Equal("0px", "24px");
    }

    [Fact]
    public async Task TableScrolledHalfway_FadesBothEdges() {
        var page = await OpenFadingGridAsync();
        await page.Locator(".nt-data-grid-scroll").EvaluateAsync("strip => strip.scrollLeft = (strip.scrollWidth - strip.clientWidth) / 2");

        await WaitForFadeAsync(page, "start", "24px");
        (await GetFadeWidthsAsync(page)).Should().Equal("24px", "24px");
    }

    [Fact]
    public async Task LeftToRightTable_MasksFromTheLeftEdge() {
        var page = await OpenFadingGridAsync();

        (await GetMaskImageAsync(page)).Should().StartWith("linear-gradient(to right,");
    }

    [Fact]
    public async Task RightToLeftTable_MasksFromTheRightEdge() {
        var page = await OpenFadingGridAsync();

        await page.Locator(".nt-data-grid-scroll").EvaluateAsync("strip => strip.dir = 'rtl'");

        (await GetMaskImageAsync(page)).Should().StartWith("linear-gradient(to left,");
    }

    [Fact]
    public async Task TableScrolledToItsEnd_FadesOnlyTheEdgeWithMoreColumns() {
        var page = await OpenFadingGridAsync();
        await page.Locator(".nt-data-grid-scroll").EvaluateAsync("strip => strip.scrollLeft = strip.scrollWidth");

        await WaitForFadeAsync(page, "end", "0px");
        (await GetFadeWidthsAsync(page)).Should().Equal("24px", "0px");
    }

    [Fact]
    public async Task TableThatFits_DoesNotFade() {
        var page = await OpenFadingGridAsync(_wideViewport, expectOverflow: false);

        (await GetFadeWidthsAsync(page)).Should().Equal("0px", "0px");
    }

    private static ILocator OrderHeader(IPage page) => page.GetByRole(AriaRole.Link, new() { Name = "Order", Exact = true });

    private async Task<IPage> OpenStaticGridAsync(int width = 390, bool expectOverflow = true) {
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(width, 844);
        await page.GotoAsync($"{_fixture.ServerAddress}/static-paginated-grid");
        // The drag handlers attach when NTComponents.lib.module.js loads, which also defines this global.
        await page.WaitForFunctionAsync("() => typeof window.NTComponents?.jumpToGridPage === 'function'");
        var widths = await page.Locator(".nt-data-grid-scroll").EvaluateAsync<double[]>("strip => [strip.scrollWidth, strip.clientWidth]");
        if (expectOverflow) {
            (widths[0] - widths[1]).Should().BeGreaterThan(_dragDistance, $"the fixture's table must be wider than the grid for a drag to scroll it (scroll {widths[0]}px, visible {widths[1]}px)");
        }
        else {
            widths[0].Should().BeLessThanOrEqualTo(widths[1], "the fixture's table must fit the grid at this width");
        }

        return page;
    }

    // Browsers without scroll-driven animations, such as Firefox, skip the fades by design.
    private async Task<IPage> OpenFadingGridAsync(int width = 390, bool expectOverflow = true) {
        var page = await OpenStaticGridAsync(width, expectOverflow);
        Assert.SkipUnless(await page.EvaluateAsync<bool>("() => CSS.supports('animation-timeline: scroll()')"), "This browser has no scroll-driven animations, so the grid does not fade its edges.");
        return page;
    }

    // Scroll timelines update on the next frame, so wait for the fade to settle before reading it.
    private static Task WaitForFadeAsync(IPage page, string edge, string width) => page.WaitForFunctionAsync(
        $"() => getComputedStyle(document.querySelector('.nt-data-grid-scroll')).getPropertyValue('--nt-data-grid-fade-{edge}').trim() === '{width}'");

    private static Task<string> GetMaskImageAsync(IPage page) => page.Locator(".nt-data-grid-scroll").EvaluateAsync<string>("strip => getComputedStyle(strip).maskImage");

    // The leading and trailing fade widths the scroll-driven animations set on the scroll container.
    private static Task<string[]> GetFadeWidthsAsync(IPage page) => page.Locator(".nt-data-grid-scroll").EvaluateAsync<string[]>(
        "strip => ['--nt-data-grid-fade-start', '--nt-data-grid-fade-end'].map(name => getComputedStyle(strip).getPropertyValue(name).trim())");

    private static async Task DragAsync(IPage page, ILocator start, int deltaX) {
        // Raw mouse input does not scroll, so bring the start point on screen first.
        await start.ScrollIntoViewIfNeededAsync();
        var box = await start.BoundingBoxAsync();
        box.Should().NotBeNull();
        var x = box!.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + deltaX, y, new() { Steps = 10 });
        await page.Mouse.UpAsync();
    }

    // The scroll container has no role, so it is addressed by its class.
    private static Task<double> StripScrollLeftAsync(IPage page) => page.Locator(".nt-data-grid-scroll").EvaluateAsync<double>("strip => strip.scrollLeft");
}
