using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Layout;

/// <summary>
///     Browser-level coverage for nested layout sizing within a parent shell.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTLayout_IntegrationTests : IAsyncLifetime {
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

    // Behavior source: NTLayout documents nested shell composition, while NTHeader and NTFooter document that their
    // default fixed state makes the sibling body the scroll container. The nested-layout contract is container-sized.
    [Fact]
    public async Task NestedLayout_With_Default_Fixed_Regions_Stays_Within_Parent_Body() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-layout-nested").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var nestedLayoutFitsParent = await _page.EvaluateAsync<bool>(
            """
            () => {
                const nested = document.querySelector('.nt-layout-nested');
                const parentBody = nested?.closest('.nt-body');
                const nestedHeader = nested?.querySelector(':scope > .nt-header-fixed-position');
                if (!(nested instanceof HTMLElement)
                    || !(parentBody instanceof HTMLElement)
                    || !(nestedHeader instanceof HTMLElement)) {
                    return false;
                }

                parentBody.style.blockSize = '360px';
                parentBody.style.maxBlockSize = '360px';
                nested.querySelector(':scope > .nt-navigation-rail')?.remove();
                nested.querySelector(':scope > .nt-navigation-rail-modal-placeholder')?.remove();

                const nestedRect = nested.getBoundingClientRect();
                const parentRect = parentBody.getBoundingClientRect();
                const headerRect = nestedHeader.getBoundingClientRect();
                return nestedRect.top >= parentRect.top - 1
                    && nestedRect.bottom <= parentRect.bottom + 1
                    && headerRect.top >= nestedRect.top - 1
                    && headerRect.bottom <= nestedRect.bottom + 1;
            }
            """);

        nestedLayoutFitsParent.Should().BeTrue(
            "a nested shell with default fixed regions must size itself to its constrained parent, not to 100dvh");
    }

    [Fact]
    public async Task NestedLayout_On_Small_Screen_Expands_Rail_Within_Its_Own_Bounds() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(700, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedLayout.EvaluateAsync(
            """
            nested => {
                const parentBody = nested.closest('.nt-body');
                if (parentBody instanceof HTMLElement) {
                    parentBody.style.blockSize = '360px';
                    parentBody.style.maxBlockSize = '360px';
                }
            }
            """);

        var nestedRail = nestedLayout.Locator(":scope > .nt-navigation-rail");
        var railId = await nestedRail.GetAttributeAsync("id");
        railId.Should().NotBeNullOrWhiteSpace();

        var menuButton = nestedRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Expand nested navigation rail", Exact = true });
        await menuButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var collapsedGeometryIsLocal = await RailFitsNestedLayoutAsync(railId!);
        collapsedGeometryIsLocal.Should().BeTrue("the visible collapsed rail should match the nested shell height");

        var nestedBodyLeft = await nestedLayout.Locator(":scope > .nt-body").EvaluateAsync<float>("element => element.getBoundingClientRect().left");
        await menuButton.ClickAsync();

        var rail = _page.Locator($"#{railId}");
        await rail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await _page.WaitForFunctionAsync(
            "id => document.getElementById(id)?.classList.contains('nt-navigation-rail-expanded') === true",
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var expandedGeometryIsLocal = await RailFitsNestedLayoutAsync(railId!);
        expandedGeometryIsLocal.Should().BeTrue("expanding a nested rail should overlay its own content instead of opening a viewport-height dialog");
        (await nestedLayout.Locator(":scope > .nt-body").EvaluateAsync<float>("element => element.getBoundingClientRect().left"))
            .Should().BeApproximately(nestedBodyLeft, 1, "the expanded rail should overlay rather than squeeze nested content");
    }

    [Fact]
    public async Task NestedLayout_On_Extra_Small_Screen_Provides_A_Local_Trigger_And_Opens_A_Modal_Rail_Without_A_Header() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(500, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedLayout.EvaluateAsync(
            """
            nested => {
                const parentBody = nested.closest('.nt-body');
                if (parentBody instanceof HTMLElement) {
                    parentBody.style.blockSize = '360px';
                    parentBody.style.maxBlockSize = '360px';
                }
            }
            """);
        await nestedLayout.Locator(":scope > .nt-header").EvaluateAsync("header => header.remove()");

        var nestedRail = nestedLayout.Locator(":scope > .nt-navigation-rail");
        var railId = await nestedRail.GetAttributeAsync("id");
        railId.Should().NotBeNullOrWhiteSpace();

        var externalMenuButton = nestedLayout.Locator($":scope > .nt-navigation-rail-xs-menu-button[aria-controls='{railId}']");
        await externalMenuButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var triggerIsLocal = await externalMenuButton.EvaluateAsync<bool>(
            """
            button => {
                const nested = button.closest('.nt-layout-nested');
                if (!(nested instanceof HTMLElement)) {
                    return false;
                }

                const buttonRect = button.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                return getComputedStyle(button).position !== 'fixed'
                    && buttonRect.top >= nestedRect.top - 1
                    && buttonRect.bottom <= nestedRect.bottom + 1;
            }
            """);
        triggerIsLocal.Should().BeTrue("a headerless nested shell needs a trigger anchored within its own layout");

        await externalMenuButton.ClickAsync();
        await _page.WaitForFunctionAsync(
            "id => document.getElementById(id)?.classList.contains('nt-navigation-rail-expanded') === true",
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var modalRailRetainsNestedBounds = await _page.EvaluateAsync<bool>(
            """
            id => {
                const rail = document.getElementById(id);
                const dialog = rail?.closest('.nt-navigation-rail-modal-dialog');
                const nested = document.querySelector('.nt-layout-nested');
                const nestedContent = document.querySelector('.nt-layout-nested > .nt-body');
                if (!(rail instanceof HTMLElement)
                    || !(dialog instanceof HTMLDialogElement)
                    || !(nested instanceof HTMLElement)
                    || !(nestedContent instanceof HTMLElement)) {
                    return false;
                }

                const dialogRect = dialog.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                const railRect = rail.getBoundingClientRect();
                return dialog.open
                    && rail.parentElement === dialog
                    && nestedContent.inert
                    && Math.abs(dialogRect.top - nestedRect.top) <= 1
                    && Math.abs(dialogRect.height - nestedRect.height) <= 1
                    && Math.abs(railRect.top - nestedRect.top) <= 1
                    && Math.abs(railRect.height - nestedRect.height) <= 1;
            }
            """,
            railId);
        modalRailRetainsNestedBounds.Should().BeTrue("the extra-small nested modal should retain the nested layout's position and height");

        await _page.SetViewportSizeAsync(700, 900);
        await _page.WaitForFunctionAsync(
            """
            id => {
                const rail = document.getElementById(id);
                return rail?.parentElement?.classList.contains('nt-layout-nested') === true
                    && rail.closest('dialog') === null
                    && rail.classList.contains('nt-navigation-rail-expanded');
            }
            """,
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.SetViewportSizeAsync(500, 900);
        await _page.WaitForFunctionAsync(
            """
            id => {
                const rail = document.getElementById(id);
                const dialog = rail?.closest('.nt-navigation-rail-modal-dialog');
                return dialog instanceof HTMLDialogElement && dialog.open;
            }
            """,
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.Locator($"#{railId}").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Accordion", Exact = true }).PressAsync("Escape");
        await _page.Locator(".nt-navigation-rail-modal-dialog").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 5000 });

        (await RailFitsNestedLayoutAsync(railId!)).Should().BeTrue("closing the modal should restore the rail to its nested layout");
        (await externalMenuButton.GetAttributeAsync("aria-expanded")).Should().Be("false");
    }

    [Fact]
    public async Task NestedLayout_Body_Does_Not_Have_Rounded_Corners() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var bodyCornerRadii = await nestedLayout.EvaluateAsync<string[]>(
            """
            nested => {
                const nestedBody = nested.querySelector(':scope > .nt-body');
                const parentBody = nested.closest('.nt-body');
                return [
                    nestedBody ? getComputedStyle(nestedBody).borderStartStartRadius : '',
                    parentBody ? getComputedStyle(parentBody).borderStartStartRadius : ''
                ];
            }
            """);

        bodyCornerRadii[0].Should().Be("0px", "nested layout bodies should have square corners");
        bodyCornerRadii[1].Should().NotBe("0px", "top-level layout body rounding should remain unchanged");
    }

    private async Task<bool> RailFitsNestedLayoutAsync(string railId) {
        ArgumentNullException.ThrowIfNull(_page);

        return await _page.EvaluateAsync<bool>(
            """
            id => {
                const rail = document.getElementById(id);
                const nested = document.querySelector('.nt-layout-nested');
                if (!(rail instanceof HTMLElement) || !(nested instanceof HTMLElement)) {
                    return false;
                }

                const railRect = rail.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                return rail.parentElement === nested
                    && rail.closest('dialog') === null
                    && railRect.top >= nestedRect.top - 1
                    && railRect.bottom <= nestedRect.bottom + 1
                    && Math.abs(railRect.height - nestedRect.height) <= 1;
            }
            """,
            railId);
    }
}
