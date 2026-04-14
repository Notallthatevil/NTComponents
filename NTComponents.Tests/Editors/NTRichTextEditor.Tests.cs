using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace NTComponents.Tests.Editors;

/// <summary>
///     Unit tests for <see cref="NTRichTextEditor" />.
/// </summary>
public class NTRichTextEditor_Tests : BunitContext {

    public NTRichTextEditor_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("focusEditor", _ => true).SetVoidResult();
        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/TnTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onDispose", _ => true).SetVoidResult();
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
    public void Type_Returns_TextArea() {
        var cut = RenderEditor();

        cut.Instance.Type.Should().Be(InputType.TextArea);
    }

    [Fact]
    public void PageScript_RenderFragment_Is_Included() {
        var cut = RenderEditor();

        var pageScripts = cut.FindAll("tnt-page-script");
        pageScripts.Should().HaveCount(6);
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/Tool/EditorToolImageButton.razor.js");
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/Tool/EditorToolTableButton.razor.js");
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/Tool/EditorToolTextColorButton.razor.js");
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/Tool/EditorToolLinkButton.razor.js");
        pageScripts.Select(script => script.GetAttribute("src")).Should().Contain("./_content/NTComponents/Editors/Tool/EditorToolIframeButton.razor.js");
    }

    [Fact]
    public void Renders_Rich_Text_Editor_Structure() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Label, "Body")
            .Add(x => x.Placeholder, "Write something"));

        cut.Find("nt-rich-text-editor").Should().NotBeNull();
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button").Should().HaveCount(26);
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-divider").Should().HaveCount(5);
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='undo']").GetAttribute("title").Should().Be("Ctrl+Z");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='redo']").GetAttribute("title").Should().Be("Ctrl+Y");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']").GetAttribute("title").Should().Be("Ctrl+B");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='unorderedList']").GetAttribute("title").Should().Be("Ctrl+Alt+7");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='strikeThrough']").GetAttribute("title").Should().Be("Ctrl+Shift+S");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='textColor']").GetAttribute("title").Should().Be("Ctrl+Alt+X");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='link']").GetAttribute("title").Should().Be("Ctrl+K");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='alignCenter']").GetAttribute("title").Should().Be("Ctrl+Shift+E");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='table']").GetAttribute("title").Should().Be("Ctrl+Alt+T");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='iframe']").GetAttribute("title").Should().Be("Ctrl+Alt+F");
        cut.Find("[data-role='image-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='image']").Should().NotBeNull();
        cut.Find("[data-role='image-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='image-file']").GetAttribute("type").Should().Be("file");
        cut.Find("[data-role='image-width']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='image-height']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='table-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='table']").Should().NotBeNull();
        cut.Find("[data-role='table-columns']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='table-rows']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='table-border-color']").GetAttribute("type").Should().Be("color");
        cut.Find("[data-role='text-color-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='textColor']").Should().NotBeNull();
        cut.Find("[data-role='text-color-value']").GetAttribute("type").Should().Be("color");
        cut.Find("[data-role='link-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='link']").Should().NotBeNull();
        cut.Find("[data-role='link-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='link-text']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-editor']").Should().NotBeNull();
        cut.Find("[data-tool-command='iframe']").Should().NotBeNull();
        cut.Find("[data-role='iframe-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='iframe-title']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-width']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-height']").GetAttribute("type").Should().Be("text");
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("type").Should().Be("hidden");
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("data-placeholder").Should().Be("Write something");
    }

    [Fact]
    public void Interactive_Renderer_Leaves_Markdown_Rendering_To_JavaScript() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Value, "This is **bold**, *italic*, <u>underline</u>, ~~crossed out~~, <span style=\"color:#2563eb;\">blue text</span>, [linked](https://example.com), and a table.\n\n| Name | Role |\n| --- | --- |\n| Avery | Host |\n\n<iframe src=\"https://example.com/embed\" title=\"Example embed\" width=\"100%\" height=\"315\" loading=\"lazy\"></iframe>"));

        cut.Find(".tnt-rich-text-editor-surface").InnerHtml.Should().BeEmpty();
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("**bold**");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("| Name | Role |");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Contain("<iframe src=\"https://example.com/embed\"");
    }

    [Fact]
    public void StaticRenderer_Renders_Enhanced_Markup_For_Ssr() {
        SetRendererInfo(new RendererInfo("Static", false));

        var model = new RichTextEditorModel();
        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.Value, "## SSR markdown")
            .Add(x => x.Placeholder, "Write markdown"));

        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("name").Should().Contain(nameof(RichTextEditorModel.Value));
        cut.Find(".tnt-rich-text-editor-hidden-input").GetAttribute("value").Should().Be("## SSR markdown");
        cut.Find(".tnt-rich-text-editor-value").TextContent.Should().Be("## SSR markdown");
        cut.Find(".tnt-rich-text-editor-toolbar").Should().NotBeNull();
        cut.Find(".tnt-rich-text-editor-surface").GetAttribute("data-placeholder").Should().Be("Write markdown");
        cut.FindAll("textarea.tnt-rich-text-editor-ssr-input").Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateValueFromJs_Updates_Value_And_Fires_BindAfter_When_BindOnInput_Is_True() {
        var bindAfterValue = string.Empty;
        var markupValue = string.Empty;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, value => markupValue = value.Value))
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValue = value ?? string.Empty)));

        await cut.Instance.UpdateValueFromJs("Updated **markdown**", "<p>Updated <strong>markdown</strong></p>");

        cut.Instance.Value.Should().Be("Updated **markdown**");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Updated <strong>markdown</strong></p>");
        markupValue.Should().Be("<p>Updated <strong>markdown</strong></p>");
        bindAfterValue.Should().Be("Updated **markdown**");
    }

    [Fact]
    public async Task CommitValueFromJs_Notifies_EditContext_And_Blur_Callback() {
        var model = new RichTextEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        var blurInvoked = false;
        var markupValue = string.Empty;

        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        var cut = Render<NTRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, value => markupValue = value.Value))
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true))
            .AddCascadingValue(editContext));

        await cut.Instance.CommitValueFromJs("Committed", "<p>Committed</p>");

        notifiedFields.Should().Contain(nameof(RichTextEditorModel.Value));
        blurInvoked.Should().BeTrue();
        cut.Instance.Value.Should().Be("Committed");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Committed</p>");
        markupValue.Should().Be("<p>Committed</p>");
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Updates_Markup_Without_Changing_Bound_Value() {
        var cut = RenderEditor(parameters => parameters.Add(x => x.Value, "Original"));

        await cut.Instance.UpdateMarkupValueFromJs("<p>Preview</p>");

        cut.Instance.Value.Should().Be("Original");
        cut.Instance.MarkupValue.Value.Should().Be("<p>Preview</p>");
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Does_Not_Reemit_Unchanged_Markup() {
        var markupChangedCount = 0;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.MarkupValueChanged, EventCallback.Factory.Create<MarkupString>(this, _ => markupChangedCount++)));

        await cut.Instance.UpdateMarkupValueFromJs("<p>Preview</p>");
        await cut.Instance.UpdateMarkupValueFromJs("<p>Preview</p>");

        markupChangedCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateMarkupValueFromJs_Normalizes_Null_To_Empty_Markup() {
        var cut = RenderEditor();

        await cut.Instance.UpdateMarkupValueFromJs(null!);

        cut.Instance.MarkupValue.Value.Should().Be(string.Empty);
    }

    [Fact]
    public async Task CommitValueFromJs_Skips_BindAfter_When_BindOnInput_And_Value_Is_Unchanged() {
        var bindAfterCount = 0;
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.BindOnInput, true)
            .Add(x => x.Value, "Same value")
            .Add(x => x.BindAfter, EventCallback.Factory.Create<string?>(this, _ => bindAfterCount++)));

        await cut.Instance.CommitValueFromJs("Same value", "<p>Same value</p>");

        bindAfterCount.Should().Be(0);
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
    public void AdditionalAttributes_Filter_FrameworkManaged_Fields_But_Preserve_Custom_Attributes() {
        var cut = RenderEditor(parameters => parameters
            .AddUnmatched("data-custom", "kept")
            .AddUnmatched("placeholder", "filtered")
            .AddUnmatched("class", "filtered-class")
            .AddUnmatched("maxlength", "25"));

        var editor = cut.Find("nt-rich-text-editor");
        editor.GetAttribute("data-custom").Should().Be("kept");
        editor.HasAttribute("placeholder").Should().BeFalse();
        editor.HasAttribute("maxlength").Should().BeFalse();
        editor.ClassName.Should().Contain("filtered-class");
    }

    [Fact]
    public void Renders_Custom_Toolbar_Buttons_From_Parameter() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Custom Bold", "Custom bold", text: "Bold"),
            new NTRichTextEditorButtonDivider(),
            new NTRichTextEditorButton("heading", "Custom Heading", "Custom heading", icon: MaterialIcon.LooksTwo, value: "2")
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button").Should().HaveCount(2);
        cut.FindAll(".tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-divider").Should().HaveCount(1);
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']").TextContent.Should().Contain("Bold");
        cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='heading']").GetAttribute("data-value").Should().Be("2");
        cut.FindAll(".tnt-rich-text-editor-toolbar-button[data-command='undo']").Should().BeEmpty();
    }

    [Fact]
    public void Renders_DefaultPlaceholder_Disabled_Icons_Tooltip_And_InputLength() {
        var cut = RenderEditor(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.StartIcon, MaterialIcon.Search)
            .Add(x => x.EndIcon, MaterialIcon.Close)
            .Add(x => x.Tooltip, builder => builder.AddContent(0, "Help"))
            .Add(x => x.Value, "Text")
            .AddUnmatched("maxlength", "10"));

        cut.Find(".tnt-start-icon").Should().NotBeNull();
        cut.Find(".tnt-end-icon").Should().NotBeNull();
        cut.Find(".tnt-tooltip-icon").Should().NotBeNull();

        var surface = cut.Find(".tnt-rich-text-editor-surface");
        surface.GetAttribute("data-placeholder").Should().Be("Start typing");
        surface.GetAttribute("contenteditable").Should().Be("false");
        surface.GetAttribute("tabindex").Should().Be("-1");
        surface.GetAttribute("aria-label").Should().Contain(nameof(RichTextEditorModel.Value));

        cut.Find(".tnt-input-length").TextContent.Should().Be("4/10");
        cut.FindAll(".tnt-rich-text-editor-toolbar-button").Should().OnlyContain(button => button.HasAttribute("disabled"));
        cut.Find("[data-role='image-url']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Toolbar_Uses_AriaLabel_Title_And_Action_Fallbacks() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Visible Title", "Explicit aria", text: "Bold"),
            new TestToolbarButton {
                Action = "italic",
                Title = null,
                AriaLabel = null,
                Text = "Italic"
            },
            new TestToolbarButton {
                Action = "underline",
                Title = null,
                AriaLabel = null,
                Icon = MaterialIcon.FormatUnderlined
            },
            new TestToolbarButton {
                Action = null,
                Title = null,
                AriaLabel = null,
                Icon = MaterialIcon.Help,
                CssClass = "toolbar-fallback-button"
            }
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        var explicitAriaButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']");
        explicitAriaButton.GetAttribute("aria-label").Should().Be("Explicit aria");
        explicitAriaButton.GetAttribute("title").Should().Be("Visible Title");

        var textFallbackButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='italic']");
        textFallbackButton.GetAttribute("aria-label").Should().Be("Italic");
        textFallbackButton.GetAttribute("title").Should().Be("Italic");

        var actionFallbackButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='underline']");
        actionFallbackButton.GetAttribute("aria-label").Should().Be("underline");
        actionFallbackButton.GetAttribute("title").Should().Be("underline");

        var defaultFallbackButton = cut.Find(".toolbar-fallback-button");
        defaultFallbackButton.GetAttribute("aria-label").Should().Be("Rich text editor action");
        defaultFallbackButton.GetAttribute("title").Should().Be("Rich text editor action");
    }

    [Fact]
    public void Filters_Rendered_Tools_To_Matching_Toolbar_Actions_And_Deduplicates_PageScripts() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Bold", "Bold", text: "Bold"),
            new NTRichTextEditorButton("image", "Image", "Image", text: "Image")
        };

        IReadOnlyList<INTRichTextEditorTool> tools = new INTRichTextEditorTool[] {
            new NTRichTextEditorTool("image", disabled => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-tool-command", "image");
                builder.AddAttribute(2, "data-test-tool", "image");
                builder.CloseElement();
            }, "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js"),
            new NTRichTextEditorTool("link", disabled => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-tool-command", "link");
                builder.AddAttribute(2, "data-test-tool", "link");
                builder.CloseElement();
            }, "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js")
        };

        var cut = RenderEditor(parameters => parameters
            .Add(x => x.ToolbarButtons, toolbarButtons)
            .Add(x => x.Tools, tools));

        cut.FindAll("[data-test-tool='image']").Should().HaveCount(1);
        cut.FindAll("[data-test-tool='link']").Should().BeEmpty();

        var pageScripts = cut.FindAll("tnt-page-script");
        pageScripts.Select(script => script.GetAttribute("src")).Should().BeEquivalentTo([
            "./_content/NTComponents/Editors/NTRichTextEditor.razor.js",
            "./_content/NTComponents/Editors/Tool/TestSharedTool.razor.js"
        ]);
    }

    [Fact]
    public void Renders_Normalized_AriaKeyShortcuts_For_Custom_Toolbar_Buttons() {
        IReadOnlyList<INTRichTextEditorButton> toolbarButtons = new INTRichTextEditorButton[] {
            new NTRichTextEditorButton("bold", "Custom Bold", "Custom bold", text: "Bold", shortcut: "Cmd+Option+K")
        };

        var cut = RenderEditor(parameters => parameters.Add(x => x.ToolbarButtons, toolbarButtons));

        var toolbarButton = cut.Find(".tnt-rich-text-editor-toolbar-button[data-command='bold']");
        toolbarButton.GetAttribute("aria-keyshortcuts").Should().Be("Meta+Alt+K");
        toolbarButton.GetAttribute("title").Should().Be("Cmd+Option+K");
    }

    [Fact]
    public async Task DisposeAsync_Calls_OnDispose_And_Clears_Interop_State() {
        var cut = RenderEditor();
        var instance = cut.Instance;

        instance.DotNetObjectRef.Should().NotBeNull();
        instance.IsolatedJsModule.Should().NotBeNull();

        await instance.DisposeAsync();

        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "onDispose");
        instance.IsolatedJsModule.Should().BeNull();
        instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_Without_Module_Still_Clears_DotNetObjectReference() {
        var instance = new NTRichTextEditor(Services.GetRequiredService<IJSRuntime>());

        instance.DotNetObjectRef.Should().NotBeNull();
        instance.IsolatedJsModule.Should().BeNull();

        await instance.DisposeAsync();

        instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public void Dispose_Synchronously_Clears_DotNetObjectReference() {
        var cut = RenderEditor();
        var instance = cut.Instance;

        instance.DotNetObjectRef.Should().NotBeNull();

        ((IDisposable)instance).Dispose();

        instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public void Dispose_False_Does_Not_Clear_DotNetObjectReference() {
        var instance = new TestableRichTextEditor(Services.GetRequiredService<IJSRuntime>());

        instance.DotNetObjectRef.Should().NotBeNull();

        instance.InvokeDispose(disposing: false);
        instance.DotNetObjectRef.Should().NotBeNull();

        instance.InvokeDispose(disposing: true);
        instance.DotNetObjectRef.Should().BeNull();

        instance.InvokeDispose(disposing: true);
        instance.DotNetObjectRef.Should().BeNull();
    }

    [Fact]
    public async Task SetFocusAsync_Invokes_FocusEditor_When_Interactive() {
        var cut = RenderEditor();

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "focusEditor");
    }

    [Fact]
    public async Task SetFocusAsync_Does_Not_Invoke_FocusEditor_When_Not_Interactive() {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = RenderEditor();

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusEditor");
    }

    [Fact]
    public async Task SetFocusAsync_Ignores_JsDisconnectedException() {
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => new RichTextEditorModel().Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.SetFocusAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_Ignores_JsDisconnectedException_From_Module() {
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => new RichTextEditorModel().Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
        cut.Instance.DotNetObjectRef.Should().BeNull();
        cut.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public async Task OnAfterRenderAsync_Ignores_JsDisconnectedException_From_OnUpdate() {
        var model = new RichTextEditorModel();
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));
        cut.Instance.SetIsolatedJsModule(new ThrowingJsModule());

        var act = () => cut.Instance.InvokeOnAfterRenderAsync(firstRender: false);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnAfterRenderAsync_Allows_Null_Module_On_Subsequent_Render() {
        var model = new RichTextEditorModel();
        var cut = Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));
        cut.Instance.ClearIsolatedJsModule();

        var act = () => cut.Instance.InvokeOnAfterRenderAsync(firstRender: false);

        await act.Should().NotThrowAsync();
        cut.Instance.IsolatedJsModule.Should().BeNull();
    }

    [Fact]
    public void OnAfterRenderAsync_Ignores_JsDisconnectedException_During_FirstRender_Load() {
        using var context = new BunitContext();
        context.SetRendererInfo(new RendererInfo("WebAssembly", true));

        var module = context.JSInterop.SetupModule("./_content/NTComponents/Editors/NTRichTextEditor.razor.js");
        module.SetupVoid("onLoad", _ => true).SetException(new JSDisconnectedException("Disconnected"));
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("focusEditor", _ => true).SetVoidResult();

        var model = new RichTextEditorModel();
        var act = () => context.Render<TestableRichTextEditor>(parameters => parameters
            .Add(x => x.ValueExpression, () => model.Value));

        act.Should().NotThrow();
    }

    private sealed class RichTextEditorModel {
        public string? Value { get; set; }
    }

    private sealed class TestToolbarButton : INTRichTextEditorButton {
        public bool IsDivider => false;
        public string? Action { get; init; }
        public string? Value { get; init; }
        public string? Title { get; init; }
        public string? Shortcut { get; init; }
        public string? AriaLabel { get; init; }
        public TnTIcon? Icon { get; init; }
        public string? Text { get; init; }
        public string? CssClass { get; init; }
    }

    private sealed class TestableRichTextEditor(IJSRuntime jsRuntime) : NTRichTextEditor(jsRuntime) {
        public void InvokeDispose(bool disposing) => base.Dispose(disposing);
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);

        public void SetIsolatedJsModule(IJSObjectReference jsObjectReference) =>
            typeof(NTRichTextEditor)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, jsObjectReference);

        public void ClearIsolatedJsModule() =>
            typeof(NTRichTextEditor)
                .GetProperty(nameof(IsolatedJsModule))!
                .SetValue(this, null);
    }

    private sealed class ThrowingJsModule : IJSObjectReference {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromException<TValue>(new JSDisconnectedException("Disconnected"));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromException<TValue>(new JSDisconnectedException("Disconnected"));
    }

    private IRenderedComponent<NTRichTextEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<NTRichTextEditor>>? configure = null) {
        var model = new RichTextEditorModel();
        return Render<NTRichTextEditor>(parameters => {
            parameters.Add(x => x.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }
}
