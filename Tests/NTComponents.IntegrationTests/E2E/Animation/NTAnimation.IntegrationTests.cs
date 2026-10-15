using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Animation;

[Collection(PlaywrightE2ECollection.Name)]
public class NTAnimation_IntegrationTests : IAsyncLifetime {

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

    private async Task OpenDemoAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(900, 700);
        await _page.GotoAsync($"{_appBaseUrl}/nt-animation", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "NTAnimation" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private Task WaitForStateAsync(string testId, string state) => _page!.WaitForFunctionAsync(
        "([id, state]) => document.querySelector(`[data-testid=\"${id}\"]`)?.getAttribute('data-nt-animation-state') === state",
        new[] { testId, state },
        new PageWaitForFunctionOptions { Timeout = 5000 });

    [Fact]
    public async Task Demo_Reveals_Content_And_Reverses_When_Scrolled_Out() {
        await OpenDemoAsync();

        (await _page!.Locator("[data-nt-animation-trigger]").CountAsync()).Should().BeGreaterThanOrEqualTo(8);
        (await _page.GetByTestId("animation-fade-in").InnerTextAsync()).Should().Contain("Fade in");

        await WaitForStateAsync("animation-fade-in", "visible");

        await _page.GetByTestId("animation-slide-out-down").ScrollIntoViewIfNeededAsync();
        await WaitForStateAsync("animation-slide-out-down", "visible");

        await _page.EvaluateAsync("() => { document.querySelector('.nt-body')?.scrollTo(0, 0); }");
        await WaitForStateAsync("animation-slide-out-down", "hidden");
    }

    [Fact]
    public async Task Stagger_Delays_Each_Child_More_Than_The_Previous() {
        await OpenDemoAsync();

        await _page!.GetByTestId("animation-stagger").ScrollIntoViewIfNeededAsync();
        await WaitForStateAsync("animation-stagger", "visible");

        var delays = await _page.EvaluateAsync<double[]>(
            """
            () => [...document.querySelectorAll('[data-testid="animation-stagger"] > li')]
                .map(li => parseFloat(getComputedStyle(li).transitionDelay.split(',').pop()))
            """);

        delays.Should().HaveCount(4);
        delays.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Manual_Trigger_Toggles_State_And_Inert() {
        await OpenDemoAsync();

        await _page!.Locator("[data-testid='animation-events'][data-interactive]").WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var manual = _page.GetByTestId("animation-manual");
        await manual.ScrollIntoViewIfNeededAsync();
        await WaitForStateAsync("animation-manual", "visible");

        var toggle = _page.GetByTestId("animation-manual-toggle");
        await toggle.ClickAsync();
        await WaitForStateAsync("animation-manual", "hidden");
        (await manual.GetAttributeAsync("inert")).Should().NotBeNull();

        await toggle.ClickAsync();
        await WaitForStateAsync("animation-manual", "visible");
        (await manual.GetAttributeAsync("inert")).Should().BeNull();
    }

    [Fact]
    public async Task OnEntered_Increments_When_Visible_Sample_Is_Revealed() {
        await OpenDemoAsync();

        await _page!.Locator("[data-testid='animation-events'][data-interactive]").WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        await _page.GetByTestId("animation-entered").ScrollIntoViewIfNeededAsync();
        await _page.WaitForFunctionAsync(
            "() => /Entered count: [1-9]/.test(document.querySelector('[data-testid=\"animation-entered-count\"]')?.textContent ?? '')",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    [Fact]
    public async Task Print_Media_Shows_Unrevealed_Content() {
        await OpenDemoAsync();

        await _page!.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });

        var opacity = await _page.EvaluateAsync<string>(
            """
            () => {
                const element = document.querySelector('[data-testid="animation-replay"]');
                return `${element.getAttribute('data-nt-animation-state')}|${getComputedStyle(element).opacity}`;
            }
            """);

        opacity.Should().EndWith("|1");
    }
}
