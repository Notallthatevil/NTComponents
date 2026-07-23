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
            "auto mode should keep the supporting pane visible at medium width");

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

    // Behavior source: NTSupportingPaneViewMode.Auto documentation says panes stack when the available width is compact.
    [Fact]
    public async Task Views_SupportingPane_Auto_Stacks_When_Host_Is_Narrow_At_Desktop_Viewport() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        await _page.EvaluateAsync(
            """
            () => {
                const view = document.querySelector('.nt-supporting-pane-view');
                const host = view?.closest('.demo-section');
                if (!(host instanceof HTMLElement)) {
                    throw new Error('Unable to find the supporting-pane demo host.');
                }

                host.style.inlineSize = '380px';
            }
            """);

        var panesAreStacked = await _page.EvaluateAsync<bool>(
            """
            () => {
                const view = document.querySelector('.nt-supporting-pane-view');
                const primary = view?.querySelector('.nt-supporting-pane-view-primary');
                const supporting = view?.querySelector('.nt-supporting-pane-view-supporting');
                if (!(view instanceof HTMLElement) || !(primary instanceof HTMLElement) || !(supporting instanceof HTMLElement)) {
                    return false;
                }

                const primaryRect = primary.getBoundingClientRect();
                const supportingRect = supporting.getBoundingClientRect();
                return view.getBoundingClientRect().width < 400
                    && supportingRect.top >= primaryRect.bottom;
            }
            """);

        panesAreStacked.Should().BeTrue(
            "Auto mode must use the width actually available to the view, even when the browser viewport is expanded");
    }

    // Behavior source: Material 3 supporting-pane guidance uses equal panes at medium width and gives the primary
    // pane approximately 70% of the available pane space at expanded width.
    [Theory]
    [InlineData(700, 0.5, "medium")]
    [InlineData(1000, 0.7, "expanded")]
    public async Task Views_SupportingPane_Auto_Uses_Material_Pane_Proportions(int inlineSize, double expectedPrimaryShare, string sizeClass) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();

        var geometry = await _page.EvaluateAsync<double[]>(
            """
            width => {
                const view = document.querySelector('.nt-supporting-pane-view');
                const primary = view?.querySelector('.nt-supporting-pane-view-primary');
                const supporting = view?.querySelector('.nt-supporting-pane-view-supporting');
                if (!(view instanceof HTMLElement) || !(primary instanceof HTMLElement) || !(supporting instanceof HTMLElement)) {
                    return [0, 0];
                }

                view.style.inlineSize = `${width}px`;

                const primaryRect = primary.getBoundingClientRect();
                const supportingRect = supporting.getBoundingClientRect();
                const panesAreSideBySide = Math.abs(primaryRect.top - supportingRect.top) <= 1
                    && primaryRect.right <= supportingRect.left;
                const primaryShare = primaryRect.width / (primaryRect.width + supportingRect.width);
                return [panesAreSideBySide ? 1 : 0, primaryShare];
            }
            """,
            inlineSize);

        geometry[0].Should().Be(1, $"Material 3 places supporting panes side by side at {sizeClass} width");
        geometry[1].Should().BeApproximately(expectedPrimaryShare, 0.02,
            $"Material 3 assigns the primary pane its {sizeClass}-width proportion");
    }

    [Fact]
    public async Task Views_SupportingPane_Stacked_Mode_Remains_Stacked_At_Expanded_Width() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await NavigateToViewsAsync();
        await _page.GetByLabel("Supporting pane controls")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Stacked" })
            .ClickAsync();

        var panesAreStacked = await _page.EvaluateAsync<bool>(
            """
            () => {
                const view = document.querySelector('.nt-supporting-pane-view');
                const primary = view?.querySelector('.nt-supporting-pane-view-primary');
                const supporting = view?.querySelector('.nt-supporting-pane-view-supporting');
                if (!(primary instanceof HTMLElement) || !(supporting instanceof HTMLElement)) {
                    return false;
                }

                const primaryRect = primary.getBoundingClientRect();
                const supportingRect = supporting.getBoundingClientRect();
                return supportingRect.top >= primaryRect.bottom;
            }
            """);

        panesAreStacked.Should().BeTrue("the explicit Stacked mode must override Material 3 adaptive proportions");
    }

    private async Task NavigateToViewsAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/views", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-supporting-pane-view").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }
}
