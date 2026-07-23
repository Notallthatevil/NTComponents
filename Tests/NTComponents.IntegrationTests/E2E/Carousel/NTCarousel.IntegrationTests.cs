using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Carousel;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTCarousel_IntegrationTests : IAsyncLifetime {
    private const string AutoPlaySectionSelector = "section[data-layout-example='autoplay']";
    private const string SettledItemOne = "Settled item: 1";
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

    // Behavior source: specs/NTCarousel-material3-implementation.md:115,349 and the explicit requirement that every configured interval visibly advances past duplicate wide-layout snap offsets.
    [Fact]
    public async Task AutoPlay_EachIntervalAdvancesToTheNextDistinctPosition() {
        await NavigateToCarouselAsync();
        await AutoPlaySection.Locator("[data-autoplay-control]").ClickAsync();
        await MovePointerOutsideAsync(AutoPlaySection);

        var trace = await CaptureSettlementsAsync(2, 11_000);

        trace.TimedOut.Should().BeFalse($"the four-second autoplay interval should settle twice within eleven seconds; observed {string.Join(", ", trace.Settlements.Select(settlement => $"{settlement.Status} at {settlement.Position:F1}px/{settlement.Elapsed:F0}ms"))}");
        trace.Settlements.Select(settlement => settlement.Status).Should().OnlyHaveUniqueItems();
        trace.Settlements[0].Position.Should().BeGreaterThan(1, "the first interval must skip the duplicate initial snap offsets and visibly move");
        trace.Settlements[1].Position.Should().BeGreaterThan(trace.Settlements[0].Position + 1, "the next interval must visibly advance again");
        trace.Settlements[0].Elapsed.Should().BeInRange(3_000, 6_000, "the first visible move should occur on the configured four-second timer, allowing browser scheduling overhead");
        (trace.Settlements[1].Elapsed - trace.Settlements[0].Elapsed).Should().BeInRange(3_000, 6_000, "the next configured interval must also result in visible movement");
    }

    // Behavior source: specs/NTCarousel-material3-implementation.md:114-115,348 and the explicit requirement to smoothly transition back to the front when reduced motion is false.
    [Fact]
    public async Task AutoPlay_TerminalReturnIsSmoothWhenReducedMotionIsNotRequested() {
        await NavigateToCarouselAsync();
        var section = AutoPlaySection;
        await MoveToFinalItemAndResumeAsync(section);

        var trace = await CaptureMotionUntilStatusAsync(section, SettledItemOne, 6_000);

        trace.TimedOut.Should().BeFalse("the terminal return should occur on the next four-second interval");
        trace.InitialPosition.Should().BeGreaterThan(1);
        trace.FinalPosition.Should().BeApproximately(0, 1);
        trace.Samples.Count(position => position > 1 && position < trace.InitialPosition - 1).Should().BeGreaterThan(3, "smooth motion must expose multiple intermediate rendered positions instead of jumping");
    }

    // Behavior source: specs/NTCarousel-material3-implementation.md:114-115,120,198,348, which require reduced motion to start paused and settle immediately.
    [Fact]
    public async Task AutoPlay_TerminalReturnIsImmediateWhenReducedMotionIsRequested() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await NavigateToCarouselAsync();
        var section = AutoPlaySection;

        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Start rotation" }).WaitForAsync();
        await MoveToFinalItemAndResumeAsync(section);
        var trace = await CaptureMotionUntilStatusAsync(section, SettledItemOne, 6_000);

        trace.TimedOut.Should().BeFalse("the explicitly resumed carousel should return on the next four-second interval");
        trace.InitialPosition.Should().BeGreaterThan(1);
        trace.FinalPosition.Should().BeApproximately(0, 1);
        trace.Samples.Should().NotContain(position => position > 1 && position < trace.InitialPosition - 1, "reduced motion must not render interpolated terminal positions");
    }

    // Behavior source: specs/NTCarousel-material3-implementation.md:198-199,280: focus stops rotation until explicit restart, while hover pauses only until the pointer leaves.
    [Fact]
    public async Task AutoPlay_FocusAndHoverExposePersistentAndTemporaryPauseSemantics() {
        ArgumentNullException.ThrowIfNull(_page);
        await NavigateToCarouselAsync();
        var section = AutoPlaySection;
        var viewport = section.Locator("[data-carousel-viewport]");
        var firstItem = section.Locator("[data-carousel-item]").First;

        await firstItem.FocusAsync();
        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Start rotation" }).WaitForAsync();
        var focusedPosition = await viewport.EvaluateAsync<double>("element => element.scrollLeft");
        await _page.WaitForTimeoutAsync(4_500);

        (await viewport.EvaluateAsync<double>("element => element.scrollLeft")).Should().BeApproximately(focusedPosition, 1, "focus must stop rotation until the user explicitly restarts it");
        (await section.Locator("output.status").TextContentAsync())?.Trim().Should().Be(SettledItemOne);

        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Start rotation" }).ClickAsync();
        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Pause rotation" }).WaitForAsync();
        await MovePointerOutsideAsync(section);
        await WaitForStatusChangeAsync(SettledItemOne, 6_000);
        var resumedStatus = (await section.Locator("output.status").TextContentAsync())?.Trim();

        await section.HoverAsync();
        var hoveredPosition = await viewport.EvaluateAsync<double>("element => element.scrollLeft");
        await _page.WaitForTimeoutAsync(4_500);

        (await viewport.EvaluateAsync<double>("element => element.scrollLeft")).Should().BeApproximately(hoveredPosition, 1, "hover must pause the active timer");
        (await section.Locator("output.status").TextContentAsync())?.Trim().Should().Be(resumedStatus);

        await MovePointerOutsideAsync(section);
        await WaitForStatusChangeAsync(resumedStatus!, 6_000);
    }

    private ILocator AutoPlaySection {
        get {
            ArgumentNullException.ThrowIfNull(_page);
            return _page.Locator(AutoPlaySectionSelector);
        }
    }

    private async Task NavigateToCarouselAsync() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.SetViewportSizeAsync(1_022, 900);
        await _page.Mouse.MoveAsync(1_010, 20);
        await _page.GotoAsync($"{_appBaseUrl}/carousel", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForFunctionAsync(
            """
            () => {
                const carousel = document.querySelector("section[data-layout-example='autoplay'] nt-carousel");
                const viewport = carousel?.querySelector('[data-carousel-viewport]');
                return customElements.get('nt-carousel') != null
                    && carousel?.dataset.enhanced === 'true'
                    && viewport?.scrollWidth > viewport?.clientWidth;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 });
        await AutoPlaySection.EvaluateAsync("section => section.scrollIntoView({ block: 'center' })");
        await MovePointerOutsideAsync(AutoPlaySection);
    }

    private async Task MoveToFinalItemAndResumeAsync(ILocator section) {
        var firstItem = section.Locator("[data-carousel-item]").First;
        await firstItem.FocusAsync();
        await firstItem.PressAsync("End");
        await WaitForStatusAsync("Settled item: 6", 5_000);
        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Start rotation" }).ClickAsync();
        await section.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Exact = true, Name = "Pause rotation" }).WaitForAsync();
        await MovePointerOutsideAsync(section);
    }

    private async Task MovePointerOutsideAsync(ILocator section) {
        ArgumentNullException.ThrowIfNull(_page);
        _ = section;
        await _page.Mouse.MoveAsync(1, 1);
    }

    private async Task WaitForStatusAsync(string expectedStatus, int timeout) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            """
            args => document.querySelector("section[data-layout-example='autoplay'] output.status")?.textContent?.trim() === args.expectedStatus
            """,
            new { expectedStatus },
            new PageWaitForFunctionOptions { Timeout = timeout });
    }

    private async Task WaitForStatusChangeAsync(string previousStatus, int timeout) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            """
            args => document.querySelector("section[data-layout-example='autoplay'] output.status")?.textContent?.trim() !== args.previousStatus
            """,
            new { previousStatus },
            new PageWaitForFunctionOptions { Timeout = timeout });
    }

    private async Task<SettlementTrace> CaptureSettlementsAsync(int expectedCount, int timeout) {
        return await AutoPlaySection.EvaluateAsync<SettlementTrace>(
            """
            (section, args) => new Promise(resolve => {
                const viewport = section.querySelector('[data-carousel-viewport]');
                const output = section.querySelector('output.status');
                const settlements = [];
                const startedAt = performance.now();
                let lastStatus = output?.textContent?.trim() ?? '';

                const sample = now => {
                    const status = output?.textContent?.trim() ?? '';
                    if (status !== lastStatus) {
                        lastStatus = status;
                        settlements.push({
                            elapsed: now - startedAt,
                            position: viewport?.scrollLeft ?? 0,
                            status
                        });
                    }

                    const completed = settlements.length >= args.expectedCount;
                    if (completed || now - startedAt >= args.timeout) {
                        resolve({ settlements, timedOut: !completed });
                        return;
                    }

                    requestAnimationFrame(sample);
                };

                requestAnimationFrame(sample);
            })
            """,
            new { expectedCount, timeout });
    }

    private static Task<MotionTrace> CaptureMotionUntilStatusAsync(ILocator section, string expectedStatus, int timeout) =>
        section.EvaluateAsync<MotionTrace>(
            """
            (section, args) => new Promise(resolve => {
                const viewport = section.querySelector('[data-carousel-viewport]');
                const output = section.querySelector('output.status');
                const initialPosition = viewport?.scrollLeft ?? 0;
                const samples = [];
                const startedAt = performance.now();

                const sample = now => {
                    const position = viewport?.scrollLeft ?? 0;
                    const status = output?.textContent?.trim() ?? '';
                    samples.push(position);
                    const completed = status === args.expectedStatus;
                    if (completed || now - startedAt >= args.timeout) {
                        resolve({
                            finalPosition: position,
                            initialPosition,
                            samples,
                            timedOut: !completed
                        });
                        return;
                    }

                    requestAnimationFrame(sample);
                };

                requestAnimationFrame(sample);
            })
            """,
            new { expectedStatus, timeout });

    private sealed class MotionTrace {
        public double FinalPosition { get; set; }
        public double InitialPosition { get; set; }
        public double[] Samples { get; set; } = [];
        public bool TimedOut { get; set; }
    }

    private sealed class Settlement {
        public double Elapsed { get; set; }
        public double Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class SettlementTrace {
        public Settlement[] Settlements { get; set; } = [];
        public bool TimedOut { get; set; }
    }
}
