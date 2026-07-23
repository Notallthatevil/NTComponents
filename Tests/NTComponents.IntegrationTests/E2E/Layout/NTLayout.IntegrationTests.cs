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
}
