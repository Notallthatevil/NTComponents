using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Typeahead;

[Collection(PlaywrightE2ECollection.Name)]
public class NTTypeahead_E2E_Tests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string _appBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        _appBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Live_Demo_Search_Selects_With_Click_And_Updates_Status() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToTypeaheadDemoAsync();

        var customerInput = _page.GetByTestId("nt-typeahead-customer");
        var customerRoot = _page.Locator(".nt-typeahead").Nth(0);
        await TypeIntoAsync(customerInput, "Ada");

        var firstOption = await WaitForFirstOptionAsync(customerRoot, customerInput);
        var optionBox = await firstOption.BoundingBoxAsync();
        optionBox.Should().NotBeNull();
        await _page.Mouse.ClickAsync(optionBox!.X + optionBox.Width / 2, optionBox.Y + optionBox.Height / 2);

        var status = _page.GetByTestId("nt-typeahead-status");
        await ExpectStatusContainsAsync("Selected Ada Lovelace");
        (await status.InnerTextAsync()).Should().Contain("Selected customer: Ada Lovelace");
        (await customerInput.InputValueAsync()).Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task Live_Demo_Tab_Selects_Active_Item_And_Moves_To_Next_Field() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToTypeaheadDemoAsync();

        var customerInput = _page.GetByTestId("nt-typeahead-customer");
        var customerRoot = _page.Locator(".nt-typeahead").Nth(0);
        var reviewerInput = _page.GetByTestId("nt-typeahead-reviewer");
        await TypeIntoAsync(customerInput, "Grace");

        await WaitForFirstOptionAsync(customerRoot, customerInput);
        await customerInput.PressAsync("Tab");

        await ExpectStatusContainsAsync("Selected Grace Hopper");
        var reviewerFocused = await reviewerInput.EvaluateAsync<bool>("element => document.activeElement === element");
        reviewerFocused.Should().BeTrue();
    }

    private async Task NavigateToTypeaheadDemoAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_appBaseUrl}/typeahead");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.GetByTestId("nt-typeahead-form").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private async Task ExpectStatusContainsAsync(string text) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            "(expected) => document.querySelector('[data-testid=\"nt-typeahead-status\"]')?.textContent?.includes(expected) === true",
            text,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    private async Task TypeIntoAsync(ILocator input, string value) {
        ArgumentNullException.ThrowIfNull(_page);

        await input.ClickAsync();
        await input.PressAsync("Control+A");
        await input.PressAsync("Backspace");
        await _page.Keyboard.TypeAsync(value, new KeyboardTypeOptions { Delay = 25 });
    }

    private async Task<ILocator> WaitForFirstOptionAsync(ILocator typeaheadRoot, ILocator input) {
        var firstOption = typeaheadRoot.Locator(".nt-combobox-option").First;
        try {
            await firstOption.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            return firstOption;
        }
        catch (TimeoutException exception) {
            var value = await input.InputValueAsync();
            var expanded = await input.GetAttributeAsync("aria-expanded");
            var rootHtml = await typeaheadRoot.InnerHTMLAsync();
            var page = input.Page;
            var statusText = await page.GetByTestId("nt-typeahead-status").InnerTextAsync();
            throw new TimeoutException($"No NTTypeahead options became visible. Input value: '{value}', aria-expanded: '{expanded}', status: {statusText}, root HTML: {rootHtml}", exception);
        }
    }
}
