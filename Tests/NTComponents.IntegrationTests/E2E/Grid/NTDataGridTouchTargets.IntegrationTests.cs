using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridTouchTargets_IntegrationTests : IAsyncLifetime {
    private const float _touchTargetSize = 48;

    // Touch emulation makes the browser report a coarse pointer, which is what the grid's touch sizing keys on.
    private readonly PlaywrightFixture _fixture = new(new() { HasTouch = true });

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task TouchScreen_PaginationDropdownsAndSortHeaderAreAtLeast48PxTall() {
        var page = await OpenStaticGridAsync(string.Empty);

        var targets = new Dictionary<string, ILocator> {
            ["Next page"] = page.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true }),
            ["Rows per page"] = page.GetByRole(AriaRole.Combobox, new() { Name = "Rows per page" }),
            ["Go to page"] = page.GetByRole(AriaRole.Combobox, new() { Name = "Go to page" }),
            ["Order sort header"] = page.GetByRole(AriaRole.Link, new() { Name = "Order", Exact = true })
        };

        foreach (var (name, target) in targets) {
            (await GetHeightAsync(target)).Should().BeGreaterThanOrEqualTo(_touchTargetSize, $"{name} is a touch target");
        }
    }

    [Fact]
    public async Task CompactDensityOnTouchScreen_KeepsCompactSortHeader() {
        var page = await OpenStaticGridAsync("?compact=true");

        var height = await GetHeightAsync(page.GetByRole(AriaRole.Link, new() { Name = "Order", Exact = true }));

        height.Should().BeLessThan(_touchTargetSize, "compact density opts out of the touch header size");
    }

    private async Task<IPage> OpenStaticGridAsync(string query) {
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(390, 900);
        await page.GotoAsync($"{_fixture.ServerAddress}/static-paginated-grid{query}");
        return page;
    }

    private static async Task<float> GetHeightAsync(ILocator target) {
        var box = await target.BoundingBoxAsync();
        box.Should().NotBeNull();
        return box!.Height;
    }
}
