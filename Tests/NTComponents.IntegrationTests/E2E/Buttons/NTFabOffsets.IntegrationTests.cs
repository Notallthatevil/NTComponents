using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Buttons;

[Collection(PlaywrightE2ECollection.Name)]
public class NTFabOffsets_IntegrationTests : IAsyncLifetime {
    private static readonly string[] UpperModernFabs = [
        ".nt-fab-button-placement-upper-right", ".nt-fab-button-placement-upper-left",
        ".nt-fab-menu-placement-upper-right", ".nt-fab-menu-placement-upper-left"
    ];
    private static readonly string[] LowerModernFabs = [
        ".nt-fab-button-placement-lower-right", ".nt-fab-button-placement-lower-left",
        ".nt-fab-menu-placement-lower-right", ".nt-fab-menu-placement-lower-left"
    ];
    private static readonly string[] UpperLegacyFabs = [".tnt-fab-container.tnt-position-topright", ".tnt-fab-container.tnt-position-topleft"];
    private static readonly string[] LowerLegacyFabs = [".tnt-fab-container.tnt-position-bottomright", ".tnt-fab-container.tnt-position-bottomleft", ".tnt-fab-button-container:not(:has(.tnt-in-container))"];

    private PlaywrightFixture? _fixture;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(390, 16)]
    [InlineData(900, 24)]
    public async Task HeaderAndFooterVariants_FabsClearRegionsAndReturnToBaseOffsetsWhenRemoved(int width, int modernGap) {
        ArgumentNullException.ThrowIfNull(_fixture);
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(width, 844);
        await page.GotoAsync($"{_fixture.ServerAddress}/fab-offsets", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(page.Locator(".nt-fab-menu")).ToHaveCountAsync(4);

        var headerBox = await page.Locator(".nt-layout > .nt-header").First.BoundingBoxAsync();
        var footerBox = await page.Locator(".nt-layout > .nt-footer").First.BoundingBoxAsync();
        ArgumentNullException.ThrowIfNull(headerBox);
        ArgumentNullException.ThrowIfNull(footerBox);

        await AssertOffsetsAsync(page, headerBox.Height + modernGap, footerBox.Height + modernGap,
            headerBox.Height + 16, footerBox.Height + 16);

        foreach (var selector in UpperModernFabs.Concat(UpperLegacyFabs)) {
            var box = await page.Locator(selector).BoundingBoxAsync();
            ArgumentNullException.ThrowIfNull(box);
            var gap = UpperModernFabs.Contains(selector) ? modernGap : 16;
            box.Y.Should().BeGreaterThanOrEqualTo(headerBox.Y + headerBox.Height + gap - 1, selector);
        }

        foreach (var selector in LowerModernFabs.Concat(LowerLegacyFabs)) {
            var box = await page.Locator(selector).BoundingBoxAsync();
            ArgumentNullException.ThrowIfNull(box);
            var gap = LowerModernFabs.Contains(selector) ? modernGap : 16;
            (box.Y + box.Height).Should().BeLessThanOrEqualTo(footerBox.Y - gap + 1, selector);
        }

        await page.EvaluateAsync("""
            () => {
                document.querySelector('.nt-layout > .nt-header').classList.replace('nt-header', 'tnt-header');
                document.querySelector('.nt-layout > .nt-footer').classList.replace('nt-footer', 'tnt-footer');
            }
            """);
        await AssertOffsetsAsync(page, headerBox.Height + modernGap, footerBox.Height + modernGap,
            headerBox.Height + 16, footerBox.Height + 16);

        await page.EvaluateAsync("""
            () => {
                document.querySelector('.nt-layout > .tnt-header').classList.remove('tnt-header');
                document.querySelector('.nt-layout > .tnt-footer').classList.remove('tnt-footer');
            }
            """);
        await AssertOffsetsAsync(page, modernGap, modernGap, 16, 16);
    }

    private static async Task AssertOffsetsAsync(IPage page, double modernTop, double modernBottom, double legacyTop, double legacyBottom) {
        foreach (var selector in UpperModernFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).top)"))
                .Should().BeApproximately(modernTop, 1, selector);
        }

        foreach (var selector in LowerModernFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).bottom)"))
                .Should().BeApproximately(modernBottom, 1, selector);
        }

        foreach (var selector in UpperLegacyFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).top)"))
                .Should().BeApproximately(legacyTop, 1, selector);
        }

        foreach (var selector in LowerLegacyFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).bottom)"))
                .Should().BeApproximately(legacyBottom, 1, selector);
        }
    }
}
