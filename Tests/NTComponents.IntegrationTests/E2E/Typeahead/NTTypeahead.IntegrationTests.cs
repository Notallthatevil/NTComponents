using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Typeahead;

[Collection(PlaywrightE2ECollection.Name)]
public class NTTypeahead_IntegrationTests : IAsyncLifetime {
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
    public async Task Live_Demo_Search_Selects_And_Clear_Button_Clears_Value() {
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

        var clearButton = customerRoot.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Clear Customer", Exact = true });
        var fieldBox = await customerRoot.Locator(".nt-input-container").BoundingBoxAsync();
        var clearButtonBox = await clearButton.BoundingBoxAsync();
        fieldBox.Should().NotBeNull();
        clearButtonBox.Should().NotBeNull();
        clearButtonBox!.X.Should().BeGreaterThan(fieldBox!.X + fieldBox.Width / 2);
        (clearButtonBox.X + clearButtonBox.Width).Should().BeLessThanOrEqualTo(fieldBox.X + fieldBox.Width);
        clearButtonBox.Y.Should().BeGreaterThanOrEqualTo(fieldBox.Y);
        (clearButtonBox.Y + clearButtonBox.Height).Should().BeLessThanOrEqualTo(fieldBox.Y + fieldBox.Height);

        await clearButton.ClickAsync();

        await ExpectStatusContainsAsync("Selected customer: None");
        (await customerInput.InputValueAsync()).Should().BeEmpty();
        (await clearButton.CountAsync()).Should().Be(0);
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

    [Fact]
    public async Task Live_Demo_Menu_Escapes_Clipped_Container_And_Selects() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToTypeaheadDemoAsync();

        var clippedFrame = _page.GetByTestId("nt-typeahead-clipped-frame");
        var clippedInput = _page.GetByTestId("nt-typeahead-clipped");
        var clippedRoot = clippedFrame.Locator(".nt-typeahead");
        await TypeIntoAsync(clippedInput, "Margaret");

        var firstOption = await WaitForFirstOptionAsync(clippedRoot, clippedInput);
        var frameBox = await clippedFrame.BoundingBoxAsync();
        var optionBox = await firstOption.BoundingBoxAsync();
        frameBox.Should().NotBeNull();
        optionBox.Should().NotBeNull();
        var escapesClippedFrame = optionBox!.Y < frameBox!.Y || optionBox.Y + optionBox.Height > frameBox.Y + frameBox.Height;
        escapesClippedFrame.Should().BeTrue("the typeahead menu should escape the clipped ancestor in either vertical direction");

        await _page.Mouse.ClickAsync(optionBox.X + optionBox.Width / 2, optionBox.Y + optionBox.Height / 2);

        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nt-typeahead-clipped-status\"]')?.textContent?.includes('Selected clipped customer: Margaret Hamilton') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        (await clippedInput.InputValueAsync()).Should().Be("Margaret Hamilton");
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

    private static Task TypeIntoAsync(ILocator input, string value) => input.FillAsync(value);

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
