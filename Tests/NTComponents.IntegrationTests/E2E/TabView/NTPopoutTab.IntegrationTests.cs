using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.TabView;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTPopoutTab_IntegrationTests : IAsyncLifetime {
    private const string PagePath = "/popout-tab";
    private static readonly string[] _labels = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight"];
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task HeaderMenuPopout_KeepsSelectionAndContentState_AndCloseReturnsTabToItsPosition() {
        var tabView = await LoadAsync();
        await GetTab(tabView, "Five").ClickAsync();
        await tabView.GetByRole(AriaRole.Tabpanel).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Count 0" }).ClickAsync();
        await GetTab(tabView, "Six").ClickAsync();

        await GetTab(tabView, "Five").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await _page!.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Open Five in a window" }).ClickAsync();

        var window = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "Five" });
        await Assertions.Expect(window).ToBeVisibleAsync();
        await Assertions.Expect(window.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Count 1" })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByTestId("popout-status")).ToHaveTextAsync("Popped out: Five");
        await Assertions.Expect(TabLabels(tabView)).ToHaveTextAsync(_labels.Where(label => label != "Five").ToArray());
        await Assertions.Expect(GetTab(tabView, "Six")).ToHaveAttributeAsync("aria-selected", "true");

        await window.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Close Five" }).ClickAsync();

        await Assertions.Expect(window).ToHaveCountAsync(0);
        await Assertions.Expect(TabLabels(tabView)).ToHaveTextAsync(_labels);
        await Assertions.Expect(GetTab(tabView, "Six")).ToHaveAttributeAsync("aria-selected", "true");
        await GetTab(tabView, "Five").ClickAsync();
        await Assertions.Expect(tabView.GetByRole(AriaRole.Tabpanel).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Count 1" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CornerButtonOnSelectedTab_SelectsNeighbourAndShowsVisibleWindow() {
        var tabView = await LoadAsync();

        await tabView.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Open One in a window" }).ClickAsync();

        await Assertions.Expect(_page!.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "One" })).ToBeVisibleAsync();
        await Assertions.Expect(GetTab(tabView, "Two")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(tabView.GetByRole(AriaRole.Tabpanel)).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task AllTabsPoppedOut_CascadeWindows_AndReturnInDeclarationOrder() {
        var tabView = await LoadAsync();

        await _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Pop out all" }).ClickAsync();

        var windows = _page.Locator("[data-nt-window='true']");
        await Assertions.Expect(windows).ToHaveCountAsync(8);
        await Assertions.Expect(tabView.GetByRole(AriaRole.Tab)).ToHaveCountAsync(0);
        await WaitForFramesAsync();
        var origins = await windows.EvaluateAllAsync<string[]>("windows => windows.filter(w => !w.hidden).map(w => { const r = w.getBoundingClientRect(); return `${Math.round(r.left)},${Math.round(r.top)}`; })");
        origins.Should().HaveCount(8).And.OnlyHaveUniqueItems("popped-out windows cascade instead of stacking on one spot");

        foreach (var label in new[] { "Six", "One", "Eight", "Three" }) {
            var window = _page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = label });
            // Cascaded windows overlap the controls of the windows beneath them, so raise the window by its uncovered edge first.
            await window.ClickAsync(new LocatorClickOptions { Position = new Position { X = 8, Y = 32 } });
            await window.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = $"Close {label}" }).ClickAsync();
        }

        await Assertions.Expect(TabLabels(tabView)).ToHaveTextAsync(new[] { "One", "Three", "Six", "Eight" });
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Return all" }).ClickAsync();
        await Assertions.Expect(TabLabels(tabView)).ToHaveTextAsync(_labels);
        await Assertions.Expect(windows).ToHaveCountAsync(0);
        await Assertions.Expect(tabView.Locator("[role='tab'][aria-selected='true']")).ToHaveCountAsync(1);
    }

    private async Task<ILocator> LoadAsync() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}{PagePath}");
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
        var tabView = _page.Locator("nt-tab-view[name='popout-tab-demo']");
        await tabView.EvaluateAsync("tabView => new Promise(resolve => { const check = () => tabView.__ntTabViewState ? resolve() : requestAnimationFrame(check); check(); })");
        await Assertions.Expect(tabView.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Open One in a window" })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        return tabView;
    }

    private Task WaitForFramesAsync() => _page!.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private static ILocator GetTab(ILocator tabView, string name) => tabView.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = name, Exact = true });

    private static ILocator TabLabels(ILocator tabView) => tabView.Locator("[role='tab'] .nt-tab-view-label");
}
