using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Layout;

/// <summary>
///     Browser-level coverage for supporting-pane view responsive visibility.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTSupportingPaneView_IntegrationTests : IAsyncLifetime {
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
    public async Task Views_SupportingPane_Modes_Update_In_Place_Without_Navigation() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(700, 900);
        await NavigateToViewsAsync();

        var controls = _page.GetByLabel("Supporting pane controls");
        var supportingPane = _page.Locator(".nt-supporting-pane-view-supporting");
        var initialUrl = _page.Url;
        await controls.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Hide on small screens" }).ClickAsync();
        await _page.WaitForFunctionAsync(
            """
            () => {
                const pane = document.querySelector('.nt-supporting-pane-view-supporting');
                return pane && getComputedStyle(pane).display === 'none';
            }
            """);

        (await supportingPane.IsVisibleAsync()).Should().BeFalse(
            "hide-on-small-screens mode should hide the supporting pane before the expanded breakpoint");
        _page.Url.Should().Be(initialUrl, "supporting-pane demo controls should not navigate");

        await controls.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Auto" }).ClickAsync();
        await _page.WaitForFunctionAsync(
            """
            () => {
                const pane = document.querySelector('.nt-supporting-pane-view-supporting');
                return pane && getComputedStyle(pane).display !== 'none';
            }
            """);

        (await supportingPane.IsVisibleAsync()).Should().BeTrue(
            "auto mode should show the supporting pane stacked before the expanded breakpoint");

        await controls.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Hide on small screens" }).ClickAsync();
        await _page.SetViewportSizeAsync(1280, 900);
        await _page.WaitForFunctionAsync(
            """
            () => {
                const pane = document.querySelector('.nt-supporting-pane-view-supporting');
                return pane && getComputedStyle(pane).display !== 'none';
            }
            """);

        (await supportingPane.IsVisibleAsync()).Should().BeTrue(
            "expanded width should reveal the supporting pane in hide-on-small-screens mode");
    }

    private async Task NavigateToViewsAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/views", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-supporting-pane-view").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }
}
