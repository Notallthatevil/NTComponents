using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Editors;

/// <summary>
///     Unit tests for <see cref="NTRichTextEditor" />.
/// </summary>
public class NTRichTextEditor_Tests : BunitContext {

    public NTRichTextEditor_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("focusEditor", _ => true).SetVoidResult();
    }

    [Fact]
    public void Component_Implements_PageScript_Component_Interface() {
        var cut = RenderEditor();

        cut.Instance.Should().BeAssignableTo<NTComponents.Interfaces.ITnTPageScriptComponent<NTRichTextEditor>>();
        cut.Instance.Should().BeAssignableTo<IAsyncDisposable>();
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void JsModulePath_Returns_Correct_Path() {
        var cut = RenderEditor();

        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
    }

    [Fact]
    public void PageScript_RenderFragment_Is_Included() {
        var cut = RenderEditor();

        cut.Find("tnt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
    }

    [Fact]
    public void Renders_Rich_Text_Editor_Structure() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Label, "Body")
            .Add(x => x.Placeholder, "Write something"));

        cut.Find("nt-rich-text-editor").Should().NotBeNull();
        cut.FindAll(".tnt-rich-text-editor-toolbar-button").Should().HaveCount(26);
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='undo']").GetAttribute("title").Should().Be("Undo (Ctrl+Z)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='redo']").GetAttribute("title").Should().Be("Redo (Ctrl+Y)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']").GetAttribute("title").Should().Be("Bold (Ctrl+B)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='unorderedList']").GetAttribute("title").Should().Be("Bulleted List (Ctrl+Alt+7)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='strikeThrough']").GetAttribute("title").Should().Be("Strikethrough (Ctrl+Shift+S)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='textColor']").GetAttribute("title").Should().Be("Text Color (Ctrl+Alt+X)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='link']").GetAttribute("title").Should().Be("Insert Link (Ctrl+K)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='alignCenter']").GetAttribute("title").Should().Be("Align Center (Ctrl+Shift+E)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='table']").GetAttribute("title").Should().Be("Insert Table (Ctrl+Alt+T)");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='iframe']").GetAttribute("title").Should().Be("Insert Iframe (Ctrl+Alt+F)");
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("type").Should().Be("hidden");
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("data-placeholder").Should().Be("Write something");
    }

    [Fact]
    public void Renders_Initial_Markdown_As_Rendered_Html() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Value, "This is **bold**, *italic*, <u>underline</u>, ~~crossed out~~, <span style=\"color:#2563eb;\">blue text</span>, [linked](https://example.com), and a table.\n\n| Name | Role |\n| --- | --- |\n| Avery | Host |\n\n<iframe src=\"https://example.com/embed\" title=\"Example embed\" width=\"100%\" height=\"315\" loading=\"lazy\"></iframe>"));

        var surface = cut.Find(".tnt-rich-text-editor-surface");
        surface.InnerHtml.Should().Contain("<strong>bold</strong>");
        surface.InnerHtml.Should().Contain("<em>italic</em>");
        surface.InnerHtml.Should().Contain("<u>underline</u>");
        surface.InnerHtml.Should().Contain("<s>crossed out</s>");
        surface.InnerHtml.Should().Contain("<span style=\"color:#2563eb;\">blue text</span>");
        surface.InnerHtml.Should().Contain("<a href=\"https://example.com\">linked</a>");
        surface.InnerHtml.Should().Contain("<table>");
        surface.InnerHtml.Should().Contain("<th>Name</th>");
        surface.InnerHtml.Should().Contain("<td>Avery</td>");
        surface.InnerHtml.Should().Contain("<iframe src=\"https://example.com/embed\" title=\"Example embed\" width=\"100%\" height=\"315\" loading=\"lazy\"></iframe>");
    }

    [Fact]
    public async Task UpdateValueFromJs_Updates_Value_And_Fires_BindAfter_When_BindOnInput_Is_True() {
        var bindAfterValue = string.Empty;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value ?? string.Empty)));

        await cut.Instance.UpdateValueFromJs("Updated **markdown**");

        cut.Instance.Value.Should().Be("Updated **markdown**");
        bindAfterValue.Should().Be("Updated **markdown**");
    }

    [Fact]
    public async Task CommitValueFromJs_Notifies_EditContext_And_Blur_Callback() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        var blurInvoked = false;

        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true))
            .AddCascadingValue(editContext));

        await cut.Instance.CommitValueFromJs("Committed");

        notifiedFields.Should().Contain(nameof(RichTextEditorModel.Value));
        blurInvoked.Should().BeTrue();
        cut.Instance.Value.Should().Be("Committed");
    }

    [Fact]
    public void SupportingText_And_ErrorMessage_Render() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.SupportingText, "Helpful text")
            .Add(x => x.ErrorMessage, "Broken"));

        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Contain("Helpful text");
        supportingText.TextContent.Should().Contain("Broken");
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));

        cut.Find(".tnt-supporting-text").Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidClass_Applies_From_EditContext() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);
        var fieldIdentifier = new FieldIdentifier(model, nameof(RichTextEditorModel.Value));

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .AddCascadingValue(editContext));

        await cut.InvokeAsync(() => {
            messageStore.Add(fieldIdentifier, "Required");
            editContext.NotifyFieldChanged(fieldIdentifier);
            editContext.NotifyValidationStateChanged();
        });

        cut.WaitForAssertion(() => cut.Find("nt-rich-text-editor").ClassList.Should().Contain("invalid"));
    }

    [Fact]
    public void HiddenInput_Uses_Bound_Field_Name() {
        var model = new RichTextEditorModel();

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));

        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("name").Should().Contain(nameof(RichTextEditorModel.Value));
    }

    [Fact]
    public void RenderMarkdown_Handles_Paragraphs_And_LineBreaks() {
        var html = NTRichTextEditor.RenderMarkdown("First line\nSecond line\n\n**Bold**");

        html.Should().Contain("<p>First line<br />Second line</p>");
        html.Should().Contain("<p><strong>Bold</strong></p>");
    }

    [Fact]
    public void RenderMarkdown_Handles_Heading_Blocks() {
        var html = NTRichTextEditor.RenderMarkdown("# Heading 1\n\n### Heading 3\n\n###### Heading 6");

        html.Should().Contain("<h1>Heading 1</h1>");
        html.Should().Contain("<h3>Heading 3</h3>");
        html.Should().Contain("<h6>Heading 6</h6>");
    }

    [Fact]
    public void RenderMarkdown_Handles_Aligned_Blocks() {
        var markdown = """
            <div align="center">
            ## Centered Heading
            </div>
            """;

        var html = NTRichTextEditor.RenderMarkdown(markdown);

        html.Should().Contain("class=\"tnt-rich-text-editor-alignment\"");
        html.Should().Contain("style=\"text-align:center;\"");
        html.Should().Contain("<h2>Centered Heading</h2>");
    }

    [Fact]
    public void RenderMarkdown_Handles_Lists_BlockQuotes_CodeBlocks_Tables_And_Images() {
        var markdown = """
            - Parent item
              - Child item
            - Another item

            1. First
            2. Second

            > Quoted line
            > - Quoted child

            ```csharp
            var total = 1;
            ```

            ~~Archived item~~

            <span style="color:#dc2626;">Critical item</span>

            | Name | Status | Notes |
            | :--- | :---: | ---: |
            | Avery | Active | 12 |

            [Reference](https://example.com/reference)

            ![Diagram](https://example.com/diagram.png)

            <iframe src="https://example.com/embed" title="Example embed" width="100%" height="315" loading="lazy"></iframe>
            """;

        var html = NTRichTextEditor.RenderMarkdown(markdown);

        html.Should().Contain("<ul>");
        html.Should().Contain("<li>Parent item<ul><li>Child item</li></ul></li>");
        html.Should().Contain("<ol>");
        html.Should().Contain("<blockquote>");
        html.Should().Contain("<pre data-language=\"csharp\"><code data-language=\"csharp\">var total = 1;</code></pre>");
        html.Should().Contain("<p><s>Archived item</s></p>");
        html.Should().Contain("<span style=\"color:#dc2626;\">Critical item</span>");
        html.Should().Contain("<table>");
        html.Should().Contain("<th style=\"text-align:left;\">Name</th>");
        html.Should().Contain("<th style=\"text-align:center;\">Status</th>");
        html.Should().Contain("<td style=\"text-align:right;\">12</td>");
        html.Should().Contain("<a href=\"https://example.com/reference\">Reference</a>");
        html.Should().Contain("<img src=\"https://example.com/diagram.png\" alt=\"Diagram\" />");
        html.Should().Contain("<iframe src=\"https://example.com/embed\" title=\"Example embed\" width=\"100%\" height=\"315\" loading=\"lazy\"></iframe>");
    }

    private sealed class RichTextEditorModel {
        public string? Value { get; set; }
    }

    private IRenderedComponent<NTRichTextEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<NTRichTextEditor>>? configure = null) {
        var model = new RichTextEditorModel();
        return Render<NTRichTextEditor>(parameters => {
            parameters.Add(x => x.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }
}
