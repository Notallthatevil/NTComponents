using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Animation;

[Collection(PlaywrightE2ECollection.Name)]
public class NTAnimation_E2E_Tests : IAsyncLifetime {

    private string _appBaseUrl = default!;
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        _appBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Demo_Enhances_Static_Content_And_Animates_Out() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(900, 700);
        await _page.GotoAsync($"{_appBaseUrl}/nt-animation", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "NTAnimation" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        (await _page.Locator("[data-nt-animation='true']").CountAsync()).Should().BeGreaterThanOrEqualTo(8);
        (await _page.GetByTestId("animation-fade-in").InnerTextAsync()).Should().Contain("Fade in");

        await _page.WaitForFunctionAsync(
            """
            () => {
                const element = document.querySelector('[data-testid="animation-fade-in"]');
                return element?.classList.contains('nt-animation-enhanced')
                    && element.classList.contains('nt-animation-visible');
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.GetByTestId("animation-slide-out-down").ScrollIntoViewIfNeededAsync();
        await _page.WaitForFunctionAsync(
            """
            () => document.querySelector('[data-testid="animation-slide-out-down"]')?.classList.contains('nt-animation-visible') === true
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.EvaluateAsync(
            """
            () => {
                document.querySelector('.nt-body')?.scrollTo(0, 0);
            }
            """);
        await _page.WaitForFunctionAsync(
            """
            () => {
                const element = document.querySelector('[data-testid="animation-slide-out-down"]');
                return element?.classList.contains('nt-animation-enhanced')
                    && element.classList.contains('nt-animation-exiting')
                    && !element.classList.contains('nt-animation-visible');
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }
}
