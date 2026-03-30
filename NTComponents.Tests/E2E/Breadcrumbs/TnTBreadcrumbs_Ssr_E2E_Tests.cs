using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Breadcrumbs;

/// <summary>
///     E2E tests for <see cref="NTComponents.TnTBreadcrumbs" /> static SSR rendering.
/// </summary>
public class TnTBreadcrumbs_Ssr_E2E_Tests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string _appBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        _appBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task StaticBreadcrumbsPage_RendersExpectedSemanticMarkup() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_appBaseUrl}/breadcrumbs-ssr",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var root = _page.Locator("[data-testid='ssr-breadcrumbs']");
        await root.WaitForAsync();

        (await root.Locator("ol > li").CountAsync()).Should().Be(3);
        (await root.Locator("[aria-current='page']").CountAsync()).Should().Be(1);
        (await root.Locator("script").CountAsync()).Should().Be(0);
        (await root.Locator("[aria-disabled='true']").CountAsync()).Should().Be(1);
        (await root.Locator("a[href='/admin/archived']").CountAsync()).Should().Be(0);
    }
}
