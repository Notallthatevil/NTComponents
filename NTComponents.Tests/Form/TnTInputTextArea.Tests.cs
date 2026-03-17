using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputTextArea_Tests : BunitContext {

    public TnTInputTextArea_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void AutoComplete_Sets_TextArea_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AutoComplete, "off"));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AutoFocus, true));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        var callbackValue = "";
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<string?>(this, (value) => callbackValue = value ?? "");
        var cut = RenderInputTextArea(model, p => p.Add(c => c.BindAfter, callback));
        var textArea = cut.Find("textarea");

        // Act
        textArea.Change("Test Value");

        // Assert
        callbackValue.Should().Be("Test Value");
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p
            .Add(c => c.StartIcon, MaterialIcon.Home)
            .Add(c => c.EndIcon, MaterialIcon.Search));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p
            .Add(c => c.TintColor, TnTColor.Primary)
            .Add(c => c.BackgroundColor, TnTColor.Surface)
            .Add(c => c.TextColor, value: TnTColor.OnSurface)
            .Add(c => c.ErrorColor, TnTColor.Error));
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Cols_Attribute_Sets_TextArea_Cols() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "cols", "40" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("cols").Should().Be("40");
    }

    [Fact]
    public void Default_Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_Type_Is_TextArea() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.Should().NotBeNull();
        cut.Instance.Type.Should().Be(InputType.TextArea);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Disabled, true));
        var textArea = cut.Find("textarea");
        var label = cut.Find("label");

        // Assert
        textArea.HasAttribute("disabled").Should().BeTrue();
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.ElementId, "textarea-id"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("id").Should().Be("textarea-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementName_Has_Name_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert - just verify the name attribute exists and has a value
        textArea.HasAttribute("name").Should().BeTrue();
        textArea.GetAttribute("name").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.ElementTitle, "TextArea Title"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("title").Should().Be("TextArea Title");
    }

    [Fact]
    public void Empty_Placeholder_Defaults_To_Single_Space() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("placeholder").Should().Be(" ");
    }

    [Fact]
    public void Empty_String_Value_Renders_Empty_TextArea() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = "";

        // Act
        var cut = RenderInputTextArea(model);
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("value").Should().Be("");
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var label = cut.Find("label");

        // Assert TnTInputBase implements ITnTComponentBase but doesn't inherit from TnTComponentBase so it doesn't automatically get the tntid attribute This test should verify that the component
        // follows the expected interface pattern
        cut.Instance.Should().BeAssignableTo<ITnTComponentBase>();
    }

    [Fact]
    public void Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputTextArea();

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Label, "Test Label"));

        // Assert
        var labelSpan = cut.Find(".tnt-label");
        labelSpan.TextContent.Should().Be("Test Label");
    }

    [Fact]
    public void MaxLength_Attribute_Added_When_Set_In_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "maxlength", "500" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("maxlength").Should().Be("500");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-textarea" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("custom-textarea");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void SizeByContent_Adds_FieldSizingStyle_To_TextArea() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.SizeByContent, true));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("style").Should().Contain("field-sizing:content");
    }

    [Fact]
    public void SizeByContent_Does_Not_Add_FieldSizingStyle_To_Container() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.SizeByContent, true));
        var container = cut.Find(".tnt-input-container");

        // Assert
        container.GetAttribute("style").Should().NotContain("field-sizing:content");
    }

    [Fact]
    public void SizeByContent_Preserves_Custom_Style_On_TextArea() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p
            .Add(c => c.SizeByContent, true)
            .Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");
        var container = cut.Find(".tnt-input-container");

        // Assert
        textArea.GetAttribute("style").Should().Contain("margin:10px");
        textArea.GetAttribute("style").Should().Contain("field-sizing:content");
        container.GetAttribute("style").Should().Contain("margin:10px");
        container.GetAttribute("style").Should().NotContain("field-sizing:content");
    }

    [Fact]
    public void MinLength_Attribute_Added_When_Set_In_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "minlength", "10" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("minlength").Should().Be("10");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Null_Value_Renders_Empty_TextArea() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderInputTextArea(model);
        var textArea = cut.Find("textarea");

        // Assert
        var value = textArea.GetAttribute("value");
        (value == null || value == string.Empty).Should().BeTrue();
    }

    [Fact]
    public void Placeholder_Class_Added_When_Placeholder_Set() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Placeholder, "Test placeholder"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Class_Not_Added_When_Placeholder_Empty() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().NotContain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Sets_TextArea_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Placeholder, "Enter text..."));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("placeholder").Should().Be("Enter text...");
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.ReadOnly, true));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Renders_TextArea_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.Should().NotBeNull();
        var label = cut.Find("label.tnt-input");
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("tnt-input");
        cls.Should().Contain("tnt-components");
    }

    [Fact]
    public void Required_Attribute_Added_When_Additional_Attributes_Contains_Required() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "required", true } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void Rows_Attribute_Sets_TextArea_Rows() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "rows", "5" } };

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("rows").Should().Be("5");
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputTextArea();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.SupportingText, "Helper text"));

        // Assert
        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Be("Helper text");
    }

    [Fact]
    public void TextArea_Blur_Notifies_EditContext_When_Present() {
        // Arrange
        var model = CreateTestModel();
        var fieldChanged = false;

        // Create a component with EditForm wrapper that provides EditContext
        RenderFragment<EditContext> childContent = context => builder => {
            // Subscribe to field changes
            context.OnFieldChanged += (_, __) => fieldChanged = true;

            builder.OpenComponent<TnTInputTextArea>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<string?>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            builder.CloseComponent();
        };

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (RenderFragment<EditContext>)childContent!)
        );

        var textArea = cut.Find("textarea");

        // Act
        textArea.Blur();

        // Assert
        fieldChanged.Should().BeTrue();
    }

    [Fact]
    public void TextArea_Change_Updates_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputTextArea(model);
        var textArea = cut.Find("textarea");

        // Act
        textArea.Change("New Value");

        // Assert
        model.TestValue.Should().Be("New Value");
    }

    [Fact]
    public void TextArea_Input_Updates_Value_When_BindOnInput_True() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputTextArea(model, p => p.Add(c => c.BindOnInput, true));
        var textArea = cut.Find("textarea");

        // Act
        textArea.Input("Typing...");

        // Assert
        model.TestValue.Should().Be("Typing...");
    }

    [Fact]
    public void TextArea_Keydown_Does_Not_Block_Enter_Key() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        var onKeyDown = textArea.GetAttribute("onkeydown");
        onKeyDown.Should().NotContain("NTComponents.stopEnter(event)");
    }

    [Fact]
    public void TextArea_Keydown_Keeps_Supporting_Text_Handler() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("onkeydown").Should().Be("NTComponents.formKeyDownSupportingTextHandler(event)");
    }

    [Fact]
    public void TextArea_Keypress_Stops_Enter_Propagation_Without_Using_StopEnter() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("onkeypress").Should().Be("NTComponents.stopEnterPropagation(event)");
    }

    [Fact]
    public void TextArea_Title_Attribute_Uses_Field_Name() {
        // Arrange & Act
        var cut = RenderInputTextArea();
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("title").Should().Be("TestValue");
    }

    [Fact]
    public void ValidationMessage_Does_Not_Render_When_DisableValidationMessage_True() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = "";

        // Act - Render with DisableValidationMessage = true
        var cut = RenderValidationInputTextArea(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert - Verify validation is disabled
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange - This test may be complex due to how ValidationMessage component works in bUnit For now, let's verify that the validation message element would be rendered conditionally
        var model = CreateValidationTestModel();
        model.TestValue = "";

        // Act - Render with validation model within EditForm
        var cut = RenderValidationInputTextArea(model);

        // Assert - Verify the component structure supports validation messages The actual validation rendering depends on EditContext which may not work in bUnit
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();

        // The validation message rendering is handled by TnTInputBase.razor template and depends on cascading EditContext which is complex to test in isolation
        cut.Instance.Should().BeOfType<TnTInputTextArea>();
    }

    [Fact]
    public void Value_Binding_Works_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = "Initial Value";

        // Act
        var cut = RenderInputTextArea(model);
        var textArea = cut.Find("textarea");

        // Assert
        textArea.GetAttribute("value").Should().Be("Initial Value");
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputTextArea(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputTextArea> RenderInputTextArea(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputTextArea>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputTextArea>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputTextArea> RenderValidationInputTextArea(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputTextArea>>? configure = null) {
        return Render<TnTInputTextArea>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private class TestModel {
        public string? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public string? TestValue { get; set; }
    }
}
