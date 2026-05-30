using Microsoft.Playwright;
using System.Text;

namespace NTComponents.Tests.E2E.FormV2;

/// <summary>
///     Browser-level coverage for the three NTFileUpload upload modes.
/// </summary>
public class NTFileUpload_E2E_Tests : IAsyncLifetime {
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
    public async Task Static_Form_Upload_Posts_Selected_File() {
        ArgumentNullException.ThrowIfNull(_page);
        await GoToUploadTestPageAsync();

        await AssertChooseFileAffordanceIsOutlinedAsync("#static-upload-version");
        await _page.Locator("#static-file-upload").SetInputFilesAsync(CreatePayload("static-upload.txt", "static form upload"));
        await _page.Locator("#static-submit").ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = await _page.Locator("body").TextContentAsync();
        body.Should().Contain("staticFile");
        body.Should().Contain("static-upload.txt");
    }

    [Fact]
    public async Task InteractiveServer_Upload_Streams_Through_Callback_And_Completes() {
        ArgumentNullException.ThrowIfNull(_page);
        await GoToUploadTestPageAsync();
        await WaitForStatusAsync("server-status", "Server ready.");

        await AssertChooseFileAffordanceIsOutlinedAsync("#server-upload-version");
        await _page.Locator("#server-file-upload").SetInputFilesAsync(CreatePayload("server-upload.txt", "server upload"));
        await WaitForUploadButtonEnabledAsync("#server-upload-version");
        await _page.Locator("#server-upload-version .nt-file-upload-action").ClickAsync();
        await WaitForFileStatusAsync("#server-upload-version", "Processing...", 30_000);
        (await _page.Locator("#server-upload-version .nt-progress[role='progressbar']").First.GetAttributeAsync("aria-valuenow")).Should().BeNull();
        await WaitForStatusAsync("server-status", "Server completed server-upload.txt", 30_000);

        (await _page.Locator("[data-testid='server-percent']").TextContentAsync()).Should().Be("100");
        (await _page.Locator("#server-upload-version .nt-progress[role='progressbar']").First.GetAttributeAsync("aria-valuenow")).Should().Be("100");
    }

    [Fact]
    public async Task InteractiveWebAssembly_Endpoint_Upload_Uses_Callback_Stream_And_Completes() {
        ArgumentNullException.ThrowIfNull(_page);
        await GoToUploadTestPageAsync();
        await WaitForStatusAsync("endpoint-status", "Endpoint ready.", 45_000);

        await AssertChooseFileAffordanceIsOutlinedAsync("#endpoint-upload-version");
        await _page.Locator("#endpoint-file-upload").SetInputFilesAsync(CreatePayload("endpoint-upload.txt", "endpoint upload"));
        await WaitForUploadButtonEnabledAsync("#endpoint-upload-version", 30_000);
        await _page.Locator("#endpoint-upload-version .nt-file-upload-action").ClickAsync();
        await WaitForFileStatusAsync("#endpoint-upload-version", "Processing...", 30_000);
        (await _page.Locator("#endpoint-upload-version .nt-progress[role='progressbar']").First.GetAttributeAsync("aria-valuenow")).Should().BeNull();
        await WaitForStatusAsync("endpoint-status", "Endpoint completed endpoint-upload.txt", 60_000);

        (await _page.Locator("[data-testid='endpoint-percent']").TextContentAsync()).Should().Be("100");
        (await _page.Locator("#endpoint-upload-version .nt-progress[role='progressbar']").First.GetAttributeAsync("aria-valuenow")).Should().Be("100");
    }

    private static FilePayload CreatePayload(string name, string content) => new() {
        Name = name,
        MimeType = "text/plain",
        Buffer = Encoding.UTF8.GetBytes(content)
    };

    private async Task AssertChooseFileAffordanceIsOutlinedAsync(string sectionSelector) {
        ArgumentNullException.ThrowIfNull(_page);
        var choose = _page.Locator($"{sectionSelector} .nt-file-upload-choose").First;
        await choose.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });

        var borderStyle = await choose.EvaluateAsync<string>("element => getComputedStyle(element).borderTopStyle");
        var borderWidth = await choose.EvaluateAsync<string>("element => getComputedStyle(element).borderTopWidth");
        var backgroundColor = await choose.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");

        borderStyle.Should().Be("solid");
        borderWidth.Should().Be("1px");
        backgroundColor.Should().BeOneOf("rgba(0, 0, 0, 0)", "transparent");
    }

    private async Task GoToUploadTestPageAsync() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.GotoAsync($"{AppBaseUrl}/nt-file-upload-integration-test");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator("h1").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    }

    private async Task WaitForStatusAsync(string testId, string expectedText, int timeout = 10_000) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            "(args) => document.querySelector(`[data-testid='${args.testId}']`)?.textContent?.includes(args.expectedText) === true",
            new { testId, expectedText },
            new PageWaitForFunctionOptions { Timeout = timeout });
    }

    private async Task WaitForFileStatusAsync(string sectionSelector, string expectedText, int timeout = 10_000) {
        ArgumentNullException.ThrowIfNull(_page);
        try {
            await _page.WaitForFunctionAsync(
                "(args) => document.querySelector(`${args.sectionSelector} .nt-file-upload-status`)?.textContent?.includes(args.expectedText) === true",
                new { sectionSelector, expectedText },
                new PageWaitForFunctionOptions { Timeout = timeout });
        }
        catch (TimeoutException exception) {
            var status = await _page.Locator($"{sectionSelector} .nt-file-upload-status").First.TextContentAsync();
            var sectionText = await _page.Locator(sectionSelector).TextContentAsync();
            throw new TimeoutException($"Timed out waiting for '{expectedText}' in '{sectionSelector}'. Current status: '{status}'. Section text: '{sectionText}'.", exception);
        }
    }

    private async Task WaitForUploadButtonEnabledAsync(string sectionSelector, int timeout = 10_000) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            "(args) => { const button = document.querySelector(`${args.sectionSelector} .nt-file-upload-action`); return button instanceof HTMLButtonElement && !button.disabled; }",
            new { sectionSelector },
            new PageWaitForFunctionOptions { Timeout = timeout });
    }
}
