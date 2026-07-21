using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.NavRail;

/// <summary>
///     Browser-level coverage for the navigation rail's JavaScript-enhanced expand/collapse behavior.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTNavigationRail_IntegrationTests : IAsyncLifetime {

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
    public async Task Rail_InitialRender_Exposes_Navigation_Links_And_Menu_Button() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        (await GetRail().IsVisibleAsync()).Should().BeTrue();

        var button = GetExpandButton();
        (await button.GetAttributeAsync("type")).Should().Be("button");
        (await button.GetAttributeAsync("href")).Should().BeNull();
        (await button.GetAttributeAsync("aria-expanded")).Should().Be("false");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();

        (await GetHomeLink().GetAttributeAsync("aria-current")).Should().Be("page");
    }

    [Fact]
    public async Task MenuButton_Click_Toggles_Accessible_Expanded_State_In_Both_Directions() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        await GetExpandButton().ClickAsync();

        var collapseButton = GetCollapseButton();
        await WaitForMenuStateAsync(expanded: true);
        (await collapseButton.GetAttributeAsync("aria-expanded")).Should().Be("true");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();

        await collapseButton.ClickAsync();

        var expandButton = GetExpandButton();
        await WaitForMenuStateAsync(expanded: false);
        (await expandButton.GetAttributeAsync("aria-expanded")).Should().Be("false");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Rail_SmallScreen_Collapsed_VisibleRail_Uses_Modal_Item_Layout() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(700, 900);
        await NavigateToRailTestPageAsync();

        await _page.WaitForFunctionAsync(
            """
            () => {
                const rail = document.querySelector('[data-testid="nav-rail-under-test"]');
                const item = document.querySelector('[data-testid="nav-rail-home-item"]');

                return rail?.classList.contains('nt-navigation-rail-responsive-modal')
                    && rail.classList.contains('nt-navigation-rail-collapsed')
                    && item !== null;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var usesModalItemLayout = await _page.EvaluateAsync<bool>(
            """
            () => {
                const item = document.querySelector('[data-testid="nav-rail-home-item"]');
                const icon = item?.querySelector('.nt-navigation-rail-item-icon');
                const label = item?.querySelector('.nt-navigation-rail-item-label');

                if (!(item instanceof HTMLElement)
                    || !(icon instanceof HTMLElement)
                    || !(label instanceof HTMLElement)
                    || !item.classList.contains('nt-navigation-rail-item-expanded')) {
                    return false;
                }

                const iconRect = icon.getBoundingClientRect();
                const labelRect = label.getBoundingClientRect();
                const iconCenter = iconRect.top + (iconRect.height / 2);
                const labelCenter = labelRect.top + (labelRect.height / 2);

                return labelRect.left > iconRect.right
                    && Math.abs(labelCenter - iconCenter) < 2;
            }
            """);

        usesModalItemLayout.Should().BeTrue(
            "a visible collapsed rail below the medium breakpoint should use the modal item layout");
    }

    [Fact]
    public async Task LiveTest_NestedLayoutNavigation_Does_Not_Dispose_Primary_Rail_Interactions() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToLiveTestHomeAsync();

        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Buttons", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/buttons");

        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Nested Layout", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/nestedLayout");

        var nestedRail = GetNestedLiveTestRail();
        await nestedRail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedRail.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Grid", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/datagrid");

        var primaryRail = GetPrimaryLiveTestRail();
        var buttonsGroup = primaryRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Buttons", Exact = true });

        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("true");

        await buttonsGroup.ClickAsync();
        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("false");

        await buttonsGroup.ClickAsync();
        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("true");

        var collapseButton = primaryRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true });
        await collapseButton.ClickAsync();

        await _page.WaitForFunctionAsync(
            """
            () => {
                const rail = document.querySelector('nav[aria-label="LiveTest primary navigation"]');
                const button = rail?.querySelector('.nt-navigation-rail-menu-button');

                return rail?.classList.contains('nt-navigation-rail-collapsed') === true
                    && button?.getAttribute('aria-expanded') === 'false';
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private ILocator GetRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "E2E primary navigation" });
    }

    private ILocator GetPrimaryLiveTestRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "LiveTest primary navigation" });
    }

    private ILocator GetNestedLiveTestRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Nested layout navigation" });
    }

    private ILocator GetExpandButton() {
        return GetRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Expand navigation rail" });
    }

    private ILocator GetCollapseButton() {
        return GetRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail" });
    }

    private ILocator GetHomeLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Home" });
    }

    private ILocator GetComponentsLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Components" });
    }

    private ILocator GetReferenceLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Reference" });
    }

    private async Task NavigateToRailTestPageAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/nav-rail-e2e-test", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await GetRail().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetExpandButton().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetHomeLink().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private async Task NavigateToLiveTestHomeAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync(AppBaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await GetPrimaryLiveTestRail().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true }).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private async Task WaitForMenuStateAsync(bool expanded) {
        ArgumentNullException.ThrowIfNull(_page);

        var expectedName = expanded ? "Collapse navigation rail" : "Expand navigation rail";
        var expectedExpanded = expanded ? "true" : "false";

        await _page.WaitForFunctionAsync(
            """
            ([expectedName, expectedExpanded]) => {
                const button = Array.from(document.querySelectorAll('button'))
                    .find(button => button.getAttribute('aria-label') === expectedName);

                return button?.getAttribute('aria-expanded') === expectedExpanded;
            }
            """,
            new[] { expectedName, expectedExpanded },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }
}
