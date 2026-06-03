using Microsoft.Playwright;

namespace NTComponents.Tests.E2E.Editors;

/// <summary>
///     Browser coverage for the rich text editor LiveTest surface across render modes and toolbar controls.
/// </summary>
[Collection("NTRichTextEditor E2E")]
public class NTRichTextEditor_E2E_Tests : IAsyncLifetime {
    private const int ExpectedDefaultToolbarButtonCount = 27;
    private const string ServerEditorTestId = "rich-text-editor-server";
    private readonly List<string> _browserDiagnostics = [];
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public static IEnumerable<object[]> ToolbarControlCases() {
        yield return [new ToolbarControlCase("undo", null, "<p>Undo text</p>", "Undo text", "Undo text", PreparedMarkdown: "**Undo text**", PrepareUndoRedo: true)];
        yield return [new ToolbarControlCase("redo", null, "<p>Redo text</p>", "Redo text", "**Redo text**", PrepareRedo: true)];
        yield return [new ToolbarControlCase("paragraph", null, "<h2>Paragraph text</h2>", "Paragraph text", "Paragraph text")];
        yield return [new ToolbarControlCase("heading", "1", "<p>Heading one</p>", "Heading one", "# Heading one")];
        yield return [new ToolbarControlCase("heading", "2", "<p>Heading two</p>", "Heading two", "## Heading two")];
        yield return [new ToolbarControlCase("heading", "3", "<p>Heading three</p>", "Heading three", "### Heading three")];
        yield return [new ToolbarControlCase("heading", "4", "<p>Heading four</p>", "Heading four", "#### Heading four")];
        yield return [new ToolbarControlCase("heading", "5", "<p>Heading five</p>", "Heading five", "##### Heading five")];
        yield return [new ToolbarControlCase("heading", "6", "<p>Heading six</p>", "Heading six", "###### Heading six")];
        yield return [new ToolbarControlCase("alignLeft", null, "<p style=\"text-align:center;\">Left text</p>", "Left text", "Left text")];
        yield return [new ToolbarControlCase("alignCenter", null, "<p>Center text</p>", "Center text", "<div align=\"center\">")];
        yield return [new ToolbarControlCase("alignRight", null, "<p>Right text</p>", "Right text", "<div align=\"right\">")];
        yield return [new ToolbarControlCase("alignJustify", null, "<p>Justified text</p>", "Justified text", "<div align=\"justify\">")];
        yield return [new ToolbarControlCase("unorderedList", null, "<p>Bullet text</p>", "Bullet text", "- Bullet text")];
        yield return [new ToolbarControlCase("orderedList", null, "<p>Numbered text</p>", "Numbered text", "1. Numbered text")];
        yield return [new ToolbarControlCase("blockquote", null, "<p>Quote text</p>", "Quote text", "> Quote text")];
        yield return [new ToolbarControlCase("codeBlock", null, "<p>const value = 1;</p>", "const value = 1;", "```")];
        yield return [new ToolbarControlCase("table", null, "<p>Table anchor</p>", "Table anchor", null, ExpectedPanelRole: "table-editor")];
        yield return [new ToolbarControlCase("bold", null, "<p>Bold text</p>", "Bold text", "**Bold text**")];
        yield return [new ToolbarControlCase("italic", null, "<p>Italic text</p>", "Italic text", "*Italic text*")];
        yield return [new ToolbarControlCase("underline", null, "<p>Underline text</p>", "Underline text", "<u>Underline text</u>")];
        yield return [new ToolbarControlCase("strikeThrough", null, "<p>Strike text</p>", "Strike text", "~~Strike text~~")];
        yield return [new ToolbarControlCase("textColor", null, "<p>Color text</p>", "Color text", null, ExpectedPanelRole: "text-color-editor")];
        yield return [new ToolbarControlCase("link", null, "<p>Link text</p>", "Link text", null, ExpectedPanelRole: "link-editor")];
        yield return [new ToolbarControlCase("image", null, "<p>Image anchor</p>", "Image anchor", null, ExpectedPanelRole: "image-editor")];
        yield return [new ToolbarControlCase("iframe", null, "<p>Iframe anchor</p>", "Iframe anchor", null, ExpectedPanelRole: "iframe-editor")];
    }

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;

        _page.Console += (_, message) => _browserDiagnostics.Add($"console:{message.Type}:{message.Text}");
        _page.PageError += (_, error) => _browserDiagnostics.Add($"pageerror:{error}");
        _page.RequestFailed += (_, request) => _browserDiagnostics.Add($"requestfailed:{request.Url}:{request.Failure}");
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task RichTextEditor_LiveTest_Renders_FormV2_Shell_And_Syncs_Value_In_All_Render_Modes() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();

        foreach (var testId in new[] { "rich-text-editor-ssr", ServerEditorTestId, "rich-text-editor-wasm" }) {
            await WaitForEditorReadyAsync(testId);

            var root = EditorRoot(testId);
            var field = FieldRoot(testId);
            (await field.GetAttributeAsync("class")).Should().Contain("nt-input");
            (await field.GetAttributeAsync("class")).Should().Contain("nt-rich-text-editor");
            (await field.GetAttributeAsync("class")).Should().Contain("nt-input-outlined");
            (await root.Locator(".tnt-rich-text-editor-toolbar-button").CountAsync()).Should().BeGreaterThanOrEqualTo(ExpectedDefaultToolbarButtonCount);
            (await root.Locator(".nt-input-outline").First.IsVisibleAsync()).Should().BeTrue();

            await SetEditorHtmlAndSelectTextAsync(testId, "<p>mode sync</p>", "mode sync");
            await WaitForHiddenValueAsync(testId, "mode sync");
        }
    }

    [Fact]
    public async Task ToolbarControls_Activate_Expected_Editor_Behavior() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);

        foreach (var values in ToolbarControlCases()) {
            var controlCase = (ToolbarControlCase)values[0];

            await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, controlCase.InitialHtml, controlCase.SelectionText);

            if (controlCase.PrepareUndoRedo) {
                await ClickToolbarButtonAsync(new("bold"));
                await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.PreparedMarkdown!);
            }

            if (controlCase.PrepareRedo) {
                await ClickToolbarButtonAsync(new("bold"));
                await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.ExpectedMarkdown!);
                await ClickToolbarButtonAsync(new("undo"));
                await WaitForHiddenValueAsync(ServerEditorTestId, "Redo text");
            }

            await ClickToolbarButtonAsync(controlCase);

            if (controlCase.ExpectedPanelRole is not null) {
                var panel = EditorRoot(ServerEditorTestId).Locator($"[data-role='{controlCase.ExpectedPanelRole}']");
                await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                (await panel.GetAttributeAsync("aria-hidden")).Should().Be("false");
                await CloseOpenPanelAsync(controlCase.ExpectedPanelRole);
                continue;
            }

            await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.ExpectedMarkdown!);
        }
    }

    [Fact]
    public async Task ToolPanelControls_Insert_Expected_Rich_Content_And_Cancel_Closes_Panels() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);

        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, "<p>Link text</p>", "Link text");
        await ClickToolbarButtonAsync(new("link"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-url']").FillAsync("https://example.com/docs");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "[Link text](https://example.com/docs)");

        await ClickToolbarButtonAsync(new("image"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-url']").FillAsync("https://example.com/image.png");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-alt']").FillAsync("Architecture diagram");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "![Architecture diagram](https://example.com/image.png)");

        await ClickToolbarButtonAsync(new("table"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='table-columns']").FillAsync("2");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='table-rows']").FillAsync("2");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='table-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<table data-border-color=\"#94a3b8\"");

        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, "<p>Color text</p>", "Color text");
        await ClickToolbarButtonAsync(new("textColor"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-value']").FillAsync("#123456");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<span style=\"color:#123456;\">Color text</span>");

        await ClickToolbarButtonAsync(new("iframe"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-url']").FillAsync("https://example.com/embed");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-title']").FillAsync("Embedded content");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-width']").FillAsync("100%");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-height']").FillAsync("315");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<iframe src=\"https://example.com/embed\"");

        await ClickToolbarButtonAsync(new("link"));
        var linkPanel = EditorRoot(ServerEditorTestId).Locator("[data-role='link-editor']");
        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-cancel']").ClickAsync();
        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    [Fact]
    public async Task Interactive_Render_Modes_Submit_Updated_Markdown() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();

        foreach (var testId in new[] { ServerEditorTestId, "rich-text-editor-wasm" }) {
            await WaitForEditorReadyAsync(testId);
            await SetEditorHtmlAndSelectTextAsync(testId, "<p>Submitted markdown</p>", "Submitted markdown");
            await WaitForHiddenValueAsync(testId, "Submitted markdown");

            await Section(testId).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).ClickAsync();
            await ExpectStatusAsync(testId, "Form is valid.");
        }
    }

    private async Task NavigateToRichTextEditorAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/rich-text-editor", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private ILocator Section(string testId) {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.Locator($"[data-testid='{testId}']");
    }

    private ILocator EditorRoot(string testId) => Section(testId).Locator("nt-rich-text-editor");

    private ILocator FieldRoot(string testId) => Section(testId).Locator(".nt-input.nt-rich-text-editor");

    private async Task WaitForEditorReadyAsync(string testId) {
        ArgumentNullException.ThrowIfNull(_page);

        try {
            await _page.WaitForFunctionAsync(
                """
                (testId) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.tnt-rich-text-editor-surface');
                    return editor?.querySelectorAll('.tnt-rich-text-editor-toolbar-button').length >= 27
                        && surface?.textContent?.includes('Meeting Notes');
                }
                """,
                testId,
                new PageWaitForFunctionOptions { Timeout = 30000 });
        }
        catch (TimeoutException ex) {
            var state = await _page.EvaluateAsync<string>(
                """
                (testId) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.tnt-rich-text-editor-surface');
                    const pageScripts = Array.from(document.querySelectorAll('tnt-page-script')).map(script => script.getAttribute('src'));
                    return JSON.stringify({
                        testId,
                        hasSection: Boolean(section),
                        hasEditor: Boolean(editor),
                        toolbarCount: editor?.querySelectorAll('.tnt-rich-text-editor-toolbar-button').length ?? 0,
                        surfaceText: surface?.textContent?.slice(0, 160) ?? null,
                        surfaceHtml: surface?.innerHTML?.slice(0, 240) ?? null,
                        hiddenValue: editor?.querySelector('.tnt-rich-text-editor-hidden-input')?.value?.slice(0, 160) ?? null,
                        pageScriptDefined: Boolean(customElements.get('tnt-page-script')),
                        pageScripts
                    });
                }
                """,
                testId);
            throw new TimeoutException($"{ex.Message}{Environment.NewLine}{state}{Environment.NewLine}{string.Join(Environment.NewLine, _browserDiagnostics)}", ex);
        }
    }

    private async Task SetEditorHtmlAndSelectTextAsync(string testId, string html, string textToSelect) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync(
            """
            ([testId, html, textToSelect]) => {
                const section = document.querySelector(`[data-testid="${testId}"]`);
                const editor = section?.querySelector('nt-rich-text-editor');
                const surface = editor?.querySelector('.tnt-rich-text-editor-surface');
                if (!(surface instanceof HTMLElement)) {
                    throw new Error(`Editor surface not found for ${testId}`);
                }

                surface.innerHTML = html;
                surface.dataset.empty = surface.textContent?.trim() ? 'false' : 'true';
                surface.focus();

                const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT);
                let selected = false;
                while (walker.nextNode()) {
                    const node = walker.currentNode;
                    const index = node.textContent?.indexOf(textToSelect) ?? -1;
                    if (index < 0) {
                        continue;
                    }

                    const range = document.createRange();
                    range.setStart(node, index);
                    range.setEnd(node, index + textToSelect.length);
                    const selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(range);
                    selected = true;
                    break;
                }

                if (!selected) {
                    throw new Error(`Text '${textToSelect}' was not found for ${testId}`);
                }

                surface.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: null }));
            }
            """,
            new[] { testId, html, textToSelect });

        await WaitForHiddenValueAsync(testId, textToSelect);
    }

    private async Task ClickToolbarButtonAsync(ToolbarControlCase controlCase) {
        var valueSelector = controlCase.Value is null ? string.Empty : $"[data-value='{controlCase.Value}']";
        var button = EditorRoot(ServerEditorTestId).Locator($".tnt-rich-text-editor-toolbar-button[data-command='{controlCase.Command}']{valueSelector}").First;
        await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        (await button.IsEnabledAsync()).Should().BeTrue();
        await button.ClickAsync();
    }

    private async Task CloseOpenPanelAsync(string panelRole) {
        var cancelButtonRole = panelRole.Replace("-editor", "-cancel", StringComparison.Ordinal);
        var panel = EditorRoot(ServerEditorTestId).Locator($"[data-role='{panelRole}']");
        await EditorRoot(ServerEditorTestId).Locator($"[data-role='{cancelButtonRole}']").ClickAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    private async Task WaitForHiddenValueAsync(string testId, string expectedValue) {
        ArgumentNullException.ThrowIfNull(_page);

        try {
            await _page.WaitForFunctionAsync(
                """
                ([testId, expectedValue]) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const value = section?.querySelector('.tnt-rich-text-editor-hidden-input')?.value ?? '';
                    return value.includes(expectedValue);
                }
                """,
                new[] { testId, expectedValue },
                new PageWaitForFunctionOptions { Timeout = 5000 });
        }
        catch (TimeoutException ex) {
            var state = await _page.EvaluateAsync<string>(
                """
                ([testId, expectedValue]) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.tnt-rich-text-editor-surface');
                    return JSON.stringify({
                        testId,
                        expectedValue,
                        hiddenValue: editor?.querySelector('.tnt-rich-text-editor-hidden-input')?.value ?? null,
                        sourceValue: editor?.querySelector('.tnt-rich-text-editor-value')?.textContent ?? null,
                        surfaceHtml: surface?.innerHTML ?? null,
                        activeElement: document.activeElement?.outerHTML?.slice(0, 240) ?? null,
                        openPanels: Array.from(editor?.querySelectorAll('[data-tool-command][aria-hidden="false"]') ?? []).map(panel => panel.getAttribute('data-role'))
                    });
                }
                """,
                new[] { testId, expectedValue });
            throw new TimeoutException($"{ex.Message}{Environment.NewLine}{state}{Environment.NewLine}{string.Join(Environment.NewLine, _browserDiagnostics)}", ex);
        }
    }

    private async Task ExpectStatusAsync(string testId, string expectedText) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.WaitForFunctionAsync(
            """
            ([testId, expectedText]) => {
                const mode = testId.replace('rich-text-editor-', '');
                return document.querySelector(`[data-testid="${mode}-status"]`)?.textContent?.includes(expectedText);
            }
            """,
            new[] { testId, expectedText },
            new PageWaitForFunctionOptions { Timeout = 10000 });
    }

    public sealed record ToolbarControlCase(string Command, string? Value = null, string InitialHtml = "<p>Text</p>", string SelectionText = "Text", string? ExpectedMarkdown = null, string? ExpectedPanelRole = null, string? PreparedMarkdown = null, bool PrepareUndoRedo = false, bool PrepareRedo = false);
}

[CollectionDefinition("NTRichTextEditor E2E", DisableParallelization = true)]
public sealed class NTRichTextEditorE2ECollectionDefinition;
