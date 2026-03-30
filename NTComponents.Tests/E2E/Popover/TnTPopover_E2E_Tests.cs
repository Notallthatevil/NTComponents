using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Popover;

/// <summary>
///     Browser tests for the popover window system.
/// </summary>
public class TnTPopover_E2E_Tests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;
    }

    [Fact]
    public async Task Popover_CanBeDragged_Hidden_AndRestored() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/popover", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.ClickAsync("#open-notes-popover");
        await _page.ClickAsync("#open-inspector-popover");
        await _page.WaitForSelectorAsync(".tnt-popover");

        var notesWindow = _page.Locator(".tnt-popover:has-text('Notes')").First;
        var inspectorWindow = _page.Locator(".tnt-popover:has-text('Inspector')").First;
        var header = notesWindow.Locator(".tnt-popover__header");

        var headerBox = await header.BoundingBoxAsync();
        headerBox.Should().NotBeNull();

        await _page.Mouse.MoveAsync(headerBox!.X + 24, headerBox.Y + 24);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(headerBox.X + 164, headerBox.Y + 104);
        await _page.Mouse.UpAsync();

        var draggedLeft = await notesWindow.EvaluateAsync<double>("element => parseFloat(getComputedStyle(element).left)");
        draggedLeft.Should().BeGreaterThan(80);

        await notesWindow.Locator("button[aria-label='Hide Notes']").ClickAsync();
        await _page.WaitForSelectorAsync("button[aria-label='Restore Notes']");
        (await _page.Locator(".tnt-popover:has-text('Notes')").CountAsync()).Should().Be(0);

        await _page.ClickAsync("button[aria-label='Restore Notes']");
        await notesWindow.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

        var notesZIndex = await notesWindow.EvaluateAsync<int>("element => parseInt(getComputedStyle(element).zIndex, 10)");
        var inspectorZIndex = await inspectorWindow.EvaluateAsync<int>("element => parseInt(getComputedStyle(element).zIndex, 10)");

        notesZIndex.Should().BeGreaterThan(inspectorZIndex);
    }
}
