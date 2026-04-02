using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace NTComponents.Tests.Editors;

/// <summary>
///     Unit tests for <see cref="TnTMarkdownEditor" />.
/// </summary>
public class TnTMarkdownEditor_Tests : BunitContext {

    public TnTMarkdownEditor_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/Editors/TnTMarkdownEditor.razor.js");
        module.SetupVoid();
    }

    [Fact]
    public void Applies_Custom_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object>
        {
            { "class", "custom-class" },
            { "data-testid", "markdown-editor" }
        };

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.AdditionalAttributes, attrs));
        var root = cut.Find("tnt-markdown-editor");

        // Assert
        root.GetAttribute("class").Should().Contain("custom-class");
        root.GetAttribute("data-testid").Should().Be("markdown-editor");
    }

    [Fact]
    public void Component_Implements_IAsyncDisposable() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.Should().BeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public void Component_Implements_IDisposable() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void ComponentIdentifier_Is_Unique_Across_Instances() {
        // Act
        var cut1 = Render<TnTMarkdownEditor>();
        var cut2 = Render<TnTMarkdownEditor>();

        // Assert
        cut1.Instance.ComponentIdentifier.Should().NotBe(cut2.Instance.ComponentIdentifier);
        cut1.Instance.ComponentIdentifier.Should().NotBeNullOrEmpty();
        cut2.Instance.ComponentIdentifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_Empty_EditorValue_When_No_Value_Or_InitialValue_Provided() {
        // Act
        var cut = Render<TnTMarkdownEditor>();
        var editorValueDiv = cut.Find("div.editor-value");

        // Assert
        editorValueDiv.TextContent.Should().BeEmpty();
    }

    [Fact]
    public void Renders_Initial_Revision_Marker() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Find("div.editor-revision").TextContent.Should().Be("0");
    }

    [Fact]
    public void Renders_Whitespace_InitialValue_When_Provided() {
        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.InitialValue, "   \n\t  "));
        var editorValueDiv = cut.Find("div.editor-value");

        // Assert
        editorValueDiv.TextContent.Should().Be("   \n\t  ");
    }

    [Fact]
    public void ElementClass_Returns_Empty_String() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.ElementClass.Should().Be(string.Empty);
    }

    [Fact]
    public void ElementId_Is_Set_On_Initialization() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.ElementId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ElementStyle_Returns_Empty_String() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.ElementStyle.Should().Be(string.Empty);
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Act
        var cut = Render<TnTMarkdownEditor>();
        var root = cut.Find("tnt-markdown-editor");

        // Assert
        root.HasAttribute("tntid").Should().BeTrue();
        root.GetAttribute("tntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Inherits_From_TnTPageScriptComponent() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.Should().BeAssignableTo<NTComponents.Core.TnTPageScriptComponent<TnTMarkdownEditor>>();
    }

    [Fact]
    public void JSInvokable_UpdateValue_Can_Be_Called_From_JavaScript() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();
        var instance = cut.Instance;

        // Act - Simulate JavaScript calling the JSInvokable method
        var method = typeof(TnTMarkdownEditor).GetMethod(nameof(TnTMarkdownEditor.UpdateValue));
        var jsInvokableAttr = method?.GetCustomAttributes(typeof(JSInvokableAttribute), false).FirstOrDefault();

        // Assert
        jsInvokableAttr.Should().NotBeNull();
        jsInvokableAttr.Should().BeOfType<JSInvokableAttribute>();
    }

    [Fact]
    public void JsModulePath_Returns_Correct_Path() {
        // Act
        var cut = Render<TnTMarkdownEditor>();

        // Assert
        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Editors/TnTMarkdownEditor.razor.js");
    }

    [Fact]
    public void PageScript_RenderFragment_Is_Included() {
        // Act
        var cut = Render<TnTMarkdownEditor>();
        var pageScript = cut.FindAll("tnt-page-script");

        // Assert
        pageScript.Should().NotBeEmpty();
        pageScript.First().GetAttribute("src").Should().Be("./_content/NTComponents/Editors/TnTMarkdownEditor.razor.js");
    }

    [Fact]
    public void RenderedHtml_Parameter_Is_Bindable() {
        // Arrange
        var renderedHtml = new MarkupString("<h1>Test Content</h1>");

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.RenderedHtml, renderedHtml));

        // Assert
        cut.Instance.RenderedHtml.Should().Be(renderedHtml);
    }

    [Fact]
    public void Renders_Basic_MarkdownEditor_Structure() {
        // Act
        var cut = Render<TnTMarkdownEditor>();
        var root = cut.Find("tnt-markdown-editor");
        var textarea = cut.Find("textarea");

        // Assert
        root.Should().NotBeNull();
        textarea.Should().NotBeNull();
        textarea.GetAttribute("style").Should().Contain("display:none");
    }

    [Fact]
    public void Renders_InitialValue_When_Provided() {
        // Arrange
        var initialValue = "# Hello World\n\nThis is **bold** text.";

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.InitialValue, initialValue));
        var editorValueDiv = cut.Find("div.editor-value");

        // Assert
        editorValueDiv.Should().NotBeNull();
        editorValueDiv.TextContent.Should().Contain("# Hello World");
        editorValueDiv.TextContent.Should().Contain("This is **bold** text.");
        editorValueDiv.GetAttribute("style").Should().Contain("display:none");
    }

    [Fact]
    public async Task UpdateValue_Extracts_Body_Content_From_Html() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue("# Test", "<html><head></head><body><h1>Test</h1><p>Content</p></body></html>");

        // Assert
        cut.Instance.RenderedHtml!.Value.Value.Should().Be("<h1>Test</h1><p>Content</p>");
    }

    [Fact]
    public async Task UpdateValue_Handles_Empty_Body() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue("", "<body></body>");

        // Assert
        cut.Instance.Value.Should().Be("");
        cut.Instance.RenderedHtml!.Value.Value.Should().Be("");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    [InlineData(null)]
    public async Task UpdateValue_Handles_Empty_Or_Null_Values(string? value) {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue(value ?? string.Empty, "<body></body>");

        // Assert
        cut.Instance.Value.Should().Be(value ?? string.Empty);
    }

    [Fact]
    public async Task UpdateValue_Handles_Html_Without_Body_Tag() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue("# Test", "<h1>Test</h1><p>Content</p>");

        // Assert When no body tag is found, the regex should not match and RenderedHtml should not be set
        cut.Instance.RenderedHtml.Should().Be(new MarkupString("<h1>Test</h1><p>Content</p>"));
    }

    [Fact]
    public async Task UpdateValue_Tracks_Client_Revision() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue("# Test", "<body><p>Content</p></body>", 3);

        // Assert
        cut.Find("div.editor-revision").TextContent.Should().Be("3");
    }

    [Fact]
    public async Task UpdateValue_Ignores_Stale_Client_Revisions() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        await cut.Instance.UpdateValue("# Newer", "<body><p>Newer</p></body>", 2);
        await cut.Instance.UpdateValue("# Older", "<body><p>Older</p></body>", 1);

        // Assert
        cut.Instance.Value.Should().Be("# Newer");
        cut.Instance.RenderedHtml!.Value.Value.Should().Be("<p>Newer</p>");
        cut.Find("div.editor-revision").TextContent.Should().Be("2");
    }

    [Fact]
    public async Task UpdateValue_Updates_RenderedHtml_And_Fires_RenderedHtmlChanged() {
        // Arrange
        var renderedHtmlChanged = false;
        var newRenderedHtml = new MarkupString();
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.RenderedHtmlChanged, EventCallback.Factory.Create<MarkupString?>(this, (v) => {
                renderedHtmlChanged = true;
                newRenderedHtml = v ?? new MarkupString();
            })));

        // Act
        await cut.Instance.UpdateValue("# New Content", "<body><h1>New Content</h1></body>");

        // Assert
        cut.Instance.RenderedHtml.Should().NotBeNull();
        cut.Instance.RenderedHtml!.Value.Value.Should().Be("<h1>New Content</h1>");
        renderedHtmlChanged.Should().BeTrue();
        newRenderedHtml.Value.Should().Be("<h1>New Content</h1>");
    }

    [Fact]
    public async Task UpdateValue_Updates_Value_And_Fires_ValueChanged() {
        // Arrange
        var valueChanged = false;
        var newValue = string.Empty;
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, (v) => {
                valueChanged = true;
                newValue = v;
            })));

        // Act
        await cut.Instance.UpdateValue("# New Content", "<body><h1>New Content</h1></body>");

        // Assert
        cut.Instance.Value.Should().Be("# New Content");
        valueChanged.Should().BeTrue();
        newValue.Should().Be("# New Content");
    }

    [Fact]
    public void Value_Parameter_Is_Bindable() {
        // Arrange
        var value = "# Test Content";

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.Value, value));

        // Assert
        cut.Instance.Value.Should().Be(value);
    }

    [Fact]
    public void EditorValue_Prefers_Value_Over_InitialValue() {
        // Arrange
        var value = "# Bound Content";
        var initialValue = "# Initial Content";

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.InitialValue, initialValue));

        // Assert
        cut.Find("div.editor-value").TextContent.Should().Be(value);
    }

    [Fact]
    public void BoundEditorValue_Ignores_InitialValue_When_Value_Is_Null() {
        // Arrange
        var model = new MarkdownEditorModel();

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.InitialValue, "# Initial Content")
            .Add(x => x.ValueExpression, () => model.Value));

        // Assert
        cut.Find("div.editor-value").TextContent.Should().BeEmpty();
    }

    [Fact]
    public async Task WithEditContext_UpdateValue_NotifiesBoundFields() {
        // Arrange
        var model = new MarkdownEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.RenderedHtmlExpression, () => model.RenderedHtml)
            .AddCascadingValue(editContext));

        // Act
        await cut.Instance.UpdateValue("# Test", "<body><p>Content</p></body>");

        // Assert
        notifiedFields.Should().Contain(nameof(MarkdownEditorModel.Value));
        notifiedFields.Should().Contain(nameof(MarkdownEditorModel.RenderedHtml));
    }

    [Fact]
    public void ValidationMessages_Render_When_EditContext_Present() {
        // Arrange
        var model = new MarkdownEditorModel();
        var editContext = new EditContext(model);

        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.RenderedHtmlExpression, () => model.RenderedHtml)
            .AddCascadingValue(editContext));

        // Act
        var supportingText = cut.FindAll(".tnt-supporting-text");

        // Assert
        supportingText.Should().ContainSingle();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange
        var supportingTextValue = "Helpful hint.";

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.SupportingText, supportingTextValue));

        // Assert
        cut.Find(".tnt-supporting-text").TextContent.Should().Contain(supportingTextValue);
    }

    [Fact]
    public void SupportingText_DoesNotRender_When_Empty_And_NoValidation() {
        // Arrange
        var cut = Render<TnTMarkdownEditor>();

        // Act
        var supportingText = cut.FindAll(".tnt-supporting-text");

        // Assert
        supportingText.Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_With_EditContext() {
        // Arrange
        var model = new MarkdownEditorModel();
        var editContext = new EditContext(model);
        var supportingTextValue = "Validation help.";

        // Act
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.RenderedHtmlExpression, () => model.RenderedHtml)
            .Add(x => x.SupportingText, supportingTextValue)
            .AddCascadingValue(editContext));

        // Assert
        cut.Find(".tnt-supporting-text").TextContent.Should().Contain(supportingTextValue);
    }

    [Fact]
    public async Task InvalidClass_Applied_When_Value_Invalid() {
        // Arrange
        var model = new MarkdownEditorModel();
        var editContext = new EditContext(model);
        var messageStore = new ValidationMessageStore(editContext);

        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.RenderedHtmlExpression, () => model.RenderedHtml)
            .AddCascadingValue(editContext));

        // Act
        await cut.InvokeAsync(() => {
            messageStore.Add(new FieldIdentifier(model, nameof(MarkdownEditorModel.Value)), "Required");
            editContext.NotifyValidationStateChanged();
        });

        // Assert
        cut.WaitForAssertion(() => cut.Find("div.tnt-markdown-editor").ClassList.Should().Contain("invalid"));
    }

    [Fact]
    public async Task WithBlurCallback_HandleBlurAsync_InvokesCallback() {
        // Arrange
        var blurInvoked = false;
        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurInvoked = true)));

        // Act
        await cut.Instance.HandleBlurAsync();

        // Assert
        blurInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task WithEditContext_HandleBlurAsync_NotifiesBoundFields() {
        // Arrange
        var model = new MarkdownEditorModel();
        var editContext = new EditContext(model);
        var notifiedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => notifiedFields.Add(args.FieldIdentifier.FieldName);

        var cut = Render<TnTMarkdownEditor>(p => p
            .Add(x => x.ValueExpression, () => model.Value)
            .Add(x => x.RenderedHtmlExpression, () => model.RenderedHtml)
            .AddCascadingValue(editContext));

        // Act
        await cut.Instance.HandleBlurAsync();

        // Assert
        notifiedFields.Should().Contain(nameof(MarkdownEditorModel.Value));
        notifiedFields.Should().Contain(nameof(MarkdownEditorModel.RenderedHtml));
    }

    private sealed class MarkdownEditorModel {
        public string? Value { get; set; }

        public MarkupString? RenderedHtml { get; set; }
    }
}
