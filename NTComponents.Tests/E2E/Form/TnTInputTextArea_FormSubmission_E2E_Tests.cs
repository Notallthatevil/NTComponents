using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Form;

/// <summary>
/// E2E tests for TnTInputTextArea Enter key behavior inside a parent submit handler.
/// </summary>
public class TnTInputTextArea_FormSubmission_E2E_Tests : IAsyncLifetime {
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

    [Fact]
    public async Task Enter_In_Regular_Input_Reaches_Parent_Submit_Handler() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/textarea-form-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var consoleMessages = new List<string>();
        _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

        var input = _page.Locator("input[placeholder='Regular input control']");
        await input.FocusAsync();
        await input.PressAsync("Enter");
        await _page.WaitForTimeoutAsync(300);

        consoleMessages.Should().Contain(message => message.Contains("Form submitted"));
    }

    [Fact]
    public async Task Enter_In_TnTInputTextArea_Does_Not_Submit_And_Inserts_Newline() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/textarea-form-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var consoleMessages = new List<string>();
        _page.Console += (_, msg) => consoleMessages.Add(msg.Text);

        var textarea = _page.Locator("textarea[placeholder='Textarea under test']");
        await textarea.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        await textarea.FocusAsync();
        await textarea.FillAsync("Line 1");
        await textarea.PressAsync("Enter");
        await _page.Keyboard.TypeAsync("Line 2");
        await _page.WaitForTimeoutAsync(300);

        consoleMessages.Should().NotContain(message => message.Contains("Form submitted"));
        (await _page.Locator(".submit-count").CountAsync()).Should().Be(0);
        (await textarea.InputValueAsync()).Should().Be("Line 1\nLine 2");
    }
}
