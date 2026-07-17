using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Typeahead;

/// <summary>
/// E2E tests for TnTTypeahead form submission behavior using Playwright.
/// These tests verify that the typeahead component correctly handles form submissions
/// in a real browser environment.
/// 
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class TnTTypeahead_FormSubmission_IntegrationTests : IAsyncLifetime {
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

    /// <summary>
    /// Test that form submission occurs normally when Enter is pressed in a regular text input (control test).
    /// This proves that the form submission prevention mechanism is meaningful and working correctly.
    /// </summary>
    [Fact]
    public async Task Enter_WithinForm_SubmitsFormWhenNoTypeaheadItemFocused() {
        // Arrange
        ArgumentNullException.ThrowIfNull(_page);

        // Navigate to the form test page
        await _page.GotoAsync($"{AppBaseUrl}/typeahead-form-test");

        // Wait for the page to be ready
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the regular text input
        var textInput = _page.Locator("input[placeholder='Enter text']");

        // Ensure the input is visible and ready
        await textInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

        // Collect console messages to detect form submission
        var consoleMessages = new List<string>();
        _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

        // Act - Focus and press Enter in the text input
        await textInput.FocusAsync();
        await _page.Keyboard.PressAsync("Enter");

        // Wait for form processing
        await _page.WaitForTimeoutAsync(500);

        // Assert
        consoleMessages.Should().Contain(msg => msg.Contains("Form submitted"),
            "Form submission should occur normally when Enter is pressed in a regular input");
    }

    /// <summary>
    /// Test that form submission is prevented when Enter selects an item in the typeahead.
    /// This is the critical behavior to ensure that selecting items via keyboard doesn't submit the form.
    /// </summary>
    [Fact]
    public async Task Enter_WithinForm_PreventsFormSubmission() {
        // Arrange
        ArgumentNullException.ThrowIfNull(_page);

        // Navigate to the form test page
        await _page.GotoAsync($"{AppBaseUrl}/typeahead-form-test");

        // Wait for the page to be ready
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the typeahead input
        var typeaheadInput = _page.Locator(".tnt-typeahead input[type='text']");

        // Ensure the input is visible and ready
        await typeaheadInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

        // Collect console messages to track item selection and form submission
        var consoleMessages = new List<string>();
        _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

        // Act - Type to trigger search results
        await typeaheadInput.FocusAsync();
        await typeaheadInput.FillAsync("ap");

        // Small delay to allow input event to be processed
        await _page.WaitForTimeoutAsync(150);

        // Wait for search results to appear (with debounce)
        var firstItem = _page.Locator(".tnt-typeahead-list-item").First;
        await firstItem.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });

        // Verify the first item is focused (has the tnt-focused class)
        var focusedItem = _page.Locator(".tnt-typeahead-list-item.tnt-focused").First;
        await focusedItem.WaitForAsync(new LocatorWaitForOptions { Timeout = 2000 });

        // Press Enter to select the focused item
        await typeaheadInput.PressAsync("Enter");

        // Wait for selection to be processed
        await _page.WaitForTimeoutAsync(500);

        // Assert - Item should be selected
        consoleMessages.Should().Contain(msg => msg.Contains("Item selected"),
            "An item should be selected when Enter is pressed on a focused typeahead item");

        // Most importantly: form should NOT be submitted
        consoleMessages.Should().NotContain(msg => msg.Contains("Form submitted"),
            "Form submission should be prevented when Enter selects an item in the typeahead");
    }

    /// <summary>
    /// Test that ArrowDown navigation moves focus to the next item in the dropdown.
    /// </summary>
    [Fact]
    public async Task ArrowDown_MovesFocusToNextItem() {
        // Arrange
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/typeahead-form-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var typeaheadInput = _page.Locator(".tnt-typeahead input[type='text']");
        await typeaheadInput.WaitForAsync();

        // Act - Type to trigger search
        await typeaheadInput.FocusAsync();
        await typeaheadInput.FillAsync("e");

        // Small delay to allow input event to be processed
        await _page.WaitForTimeoutAsync(150);

        // Wait for results to appear
        var items = _page.Locator(".tnt-typeahead-list-item");
        await items.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });

        // Get the text of the initially focused item
        var initiallyFocusedText = await _page.Locator(".tnt-typeahead-list-item.tnt-focused").First
            .TextContentAsync();

        // Press ArrowDown
        await typeaheadInput.PressAsync("ArrowDown");
        await _page.WaitForTimeoutAsync(200);

        // Get the text of the now-focused item
        var nowFocusedText = await _page.Locator(".tnt-typeahead-list-item.tnt-focused").First
            .TextContentAsync();

        // Assert
        nowFocusedText.Should().NotBe(initiallyFocusedText,
            "Focus should move to the next item when ArrowDown is pressed");
    }

    /// <summary>
    /// Test that ArrowUp navigation moves focus to the previous item in the dropdown.
    /// </summary>
    [Fact]
    public async Task ArrowUp_MovesFocusToPreviousItem() {
        // Arrange
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/typeahead-form-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var typeaheadInput = _page.Locator(".tnt-typeahead input[type='text']");
        await typeaheadInput.WaitForAsync();

        // Act - Type to trigger search
        await typeaheadInput.FocusAsync();
        await typeaheadInput.FillAsync("e");

        // Small delay to allow input event to be processed
        await _page.WaitForTimeoutAsync(150);

        // Wait for results to appear
        var items = _page.Locator(".tnt-typeahead-list-item");
        await items.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });

        // Navigate down first
        await typeaheadInput.PressAsync("ArrowDown");
        await _page.WaitForTimeoutAsync(200);
        var afterDownText = await _page.Locator(".tnt-typeahead-list-item.tnt-focused").First
            .TextContentAsync();

        // Press ArrowUp
        await typeaheadInput.PressAsync("ArrowUp");
        await _page.WaitForTimeoutAsync(200);

        // Get the text of the focused item after ArrowUp
        var afterUpText = await _page.Locator(".tnt-typeahead-list-item.tnt-focused").First
            .TextContentAsync();

        // Assert
        afterUpText.Should().NotBe(afterDownText,
            "Focus should move to the previous item when ArrowUp is pressed");
    }

    /// <summary>
    /// Test that clicking an item in the dropdown selects it.
    /// </summary>
    [Fact]
    public async Task ItemClick_SelectsItem() {
        // Arrange
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/typeahead-form-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var typeaheadInput = _page.Locator(".tnt-typeahead input[type='text']");
        await typeaheadInput.WaitForAsync();

        // Collect console messages
        var consoleMessages = new List<string>();
        _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

        // Act - Type to trigger search
        await typeaheadInput.FocusAsync();
        await typeaheadInput.FillAsync("ap");

        // Small delay to allow input event to be processed
        await _page.WaitForTimeoutAsync(150);

        // Wait for results to appear
        var firstItem = _page.Locator(".tnt-typeahead-list-item").First;
        await firstItem.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });

        // Get the text of the item we're about to click
        var itemText = await firstItem.TextContentAsync();

        // Click the item
        await firstItem.ClickAsync();

        // Wait for the dropdown to disappear (indicating selection)
        var listContent = _page.Locator(".tnt-typeahead-content");
        await listContent.WaitForAsync(new LocatorWaitForOptions {
            State = WaitForSelectorState.Hidden,
            Timeout = 3000
        });

        // Assert
        itemText.Should().Contain("Apple",
            "The first item in the 'ap' search should contain 'Apple'");

        consoleMessages.Should().Contain(msg => msg.Contains("Item selected"),
            "Selecting an item should trigger the ItemSelectedCallback");
    }
}
