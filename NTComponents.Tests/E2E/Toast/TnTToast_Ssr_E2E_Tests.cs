using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Toast;

/// <summary>
///     E2E tests for <see cref="NTComponents.TnTToast" /> SSR rendering behavior.
///     Verifies that when <c>RendererInfo.IsInteractive</c> is <see langword="false" />, the component:
///     <list type="bullet">
///         <item>Injects a <c>&lt;script&gt;</c> using actual rendered element IDs and a numeric timeout value.</item>
///         <item>Produces syntactically valid JavaScript that executes in the browser.</item>
///         <item>Renders the close button with an inline <c>onclick</c> attribute instead of a Blazor event handler.</item>
///         <item>Auto-dismisses the toast after the configured timeout.</item>
///     </list>
/// </summary>
public class TnTToast_Ssr_E2E_Tests : IAsyncLifetime {

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
    public async Task Script_WhenToastHasTimeout_ContainsActualElementId_NotLiteralPlaceholder() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateAndWaitForToastAsync();

        var scriptContent = await _page.EvaluateAsync<string?>(
            "() => document.querySelector('.tnt-toast script')?.textContent");

        scriptContent.Should().NotBeNull("a <script> tag must be present inside the SSR toast");
        scriptContent.Should().NotContain("{metadata.Id}",
            "the Razor @(metadata.Id) expression must be evaluated to an actual element ID");
        scriptContent.Should().MatchRegex(@"#tnt_[0-9a-f]+",
            "the script selector must reference the rendered tnt_ prefixed element ID");
    }

    [Fact]
    public async Task Script_WhenToastHasTimeout_ContainsNumericTimeout_NotExpression() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateAndWaitForToastAsync();

        var scriptContent = await _page.EvaluateAsync<string?>(
            "() => document.querySelector('.tnt-toast script')?.textContent");

        scriptContent.Should().NotBeNull("a <script> tag must be present inside the SSR toast");
        scriptContent.Should().NotContain("{toast.Timeout * 1000}",
            "the Razor @(toast.Timeout * 1000) expression must be evaluated to a numeric millisecond value");
        scriptContent.Should().Contain("3000",
            "timeout: 3 in ShowAsync must render as 3000 ms in the setTimeout call");
    }

    [Fact]
    public async Task Script_WhenToastHasTimeout_IsSyntacticallyValidJavaScript() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateAndWaitForToastAsync();

        var isValid = await _page.EvaluateAsync<bool>("""
            () => {
                const scriptEl = document.querySelector('.tnt-toast script');
                if (!scriptEl) return false;
                try { new Function(scriptEl.textContent); return true; }
                catch { return false; }
            }
            """);

        isValid.Should().BeTrue(
            "the injected setTimeout script must be syntactically valid JavaScript with no Razor placeholder artifacts");
    }

    [Fact]
    public async Task CloseButton_InSsrMode_HasInlineOnclickHandler() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateAndWaitForToastAsync();

        var onclickValue = await _page.EvaluateAsync<string?>(
            "() => document.querySelector('.tnt-toast-header .tnt-image-button')?.getAttribute('onclick')");

        onclickValue.Should().NotBeNullOrEmpty(
            "in SSR mode the close button must carry an inline onclick attribute instead of a Blazor event handler");
        onclickValue.Should().Contain("tnt-closing",
            "the onclick handler must apply the tnt-closing CSS class to animate the dismissal");
    }

    [Fact]
    public async Task Toast_WhenHasTimeout_AutoDismissesAfterDelay() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateAndWaitForToastAsync();

        // Verify the toast is initially present
        (await _page.Locator(".tnt-toast").CountAsync()).Should().BeGreaterThan(0);

        // The injected script calls setTimeout with 3000 ms then removes the element after another 500 ms.
        // Allow generous headroom for CI environments.
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('.tnt-toast') === null",
            new PageWaitForFunctionOptions { Timeout = 8000 });
    }

    // --- helpers ---

    private async Task NavigateAndWaitForToastAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/toast-ssr-demo",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForSelectorAsync(".tnt-toast",
            new PageWaitForSelectorOptions { Timeout = 5000 });
    }
}
