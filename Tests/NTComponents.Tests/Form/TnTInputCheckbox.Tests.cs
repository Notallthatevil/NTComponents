using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputCheckbox_Tests : BunitContext {

    public TnTInputCheckbox_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void AutoComplete_Sets_Input_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.AutoComplete, "off"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.AutoFocus, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        var callbackValue = false;
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<bool>(this, (value) => callbackValue = value);
        var cut = RenderInputCheckbox(model, p => p.Add(c => c.BindAfter, callback));
        var input = cut.Find("input");

        // Act
        input.Change(true);

        // Assert
        callbackValue.Should().BeTrue();
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p
            .Add(c => c.StartIcon, MaterialIcon.Home)
            .Add(c => c.EndIcon, MaterialIcon.Search));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Checkbox_Change_Updates_Value_To_False() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = true;
        var cut = RenderInputCheckbox(model);
        var input = cut.Find("input");

        // Act
        input.Change(false);

        // Assert
        model.TestValue.Should().BeFalse();
    }

    [Fact]
    public void Checkbox_Change_Updates_Value_To_True() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = false;
        var cut = RenderInputCheckbox(model);
        var input = cut.Find("input");

        // Act
        input.Change(true);

        // Assert
        model.TestValue.Should().BeTrue();
    }

    [Fact]
    public void Checkbox_Overlay_Span_Renders() {
        // Arrange & Act
        var cut = RenderInputCheckbox();

        // Assert
        var overlay = cut.Find(".tnt-checkbox-overlay");
        overlay.Should().NotBeNull();
    }

    [Fact]
    public void Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p
            .Add(c => c.TintColor, TnTColor.Primary)
            .Add(c => c.BackgroundColor, TnTColor.Surface)
            .Add(c => c.TextColor, TnTColor.OnSurface)
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
    public void Default_Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_Type_Is_Checkbox() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("type").Should().Be("checkbox");
        cut.Instance.Type.Should().Be(InputType.Checkbox);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Disabled, true));
        var input = cut.Find("input");
        var label = cut.Find("label");

        // Assert
        input.HasAttribute("disabled").Should().BeTrue();
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.ElementId, "checkbox-id"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("id").Should().Be("checkbox-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.ElementTitle, "Checkbox Title"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("title").Should().Be("Checkbox Title");
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var label = cut.Find("label");

        // Assert
        cut.Instance.Should().BeAssignableTo<INTComponentBase>();
    }

    [Fact]
    public void Input_Blur_Notifies_EditContext_When_Present() {
        // Arrange
        var model = CreateTestModel();
        var fieldChanged = false;

        // Create a component with EditForm wrapper that provides EditContext
        RenderFragment<EditContext> childContent = context => builder => {
            // Subscribe to field changes
            context.OnFieldChanged += (_, __) => fieldChanged = true;

            builder.OpenComponent<TnTInputCheckbox>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<bool>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<bool>(this, v => model.TestValue = v));
            builder.CloseComponent();
        };

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (RenderFragment<EditContext>)childContent!)
        );

        var input = cut.Find("input");

        // Act
        input.Blur();

        // Assert
        fieldChanged.Should().BeTrue();
    }

    [Fact]
    public void Input_Title_Attribute_Uses_Field_Name() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("title").Should().Be("TestValue");
    }

    [Fact]
    public void Input_Value_Always_TrueString() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var input = cut.Find("input");

        // Assert - checkbox inputs always have value="True" in HTML
        input.GetAttribute("value").Should().Be("True");
    }

    [Fact]
    public void Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputCheckbox();

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Label, "Test Label"));

        // Assert
        var labelSpan = cut.Find(".tnt-label");
        labelSpan.TextContent.Should().Be("Test Label");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-checkbox" } };

        // Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("custom-checkbox");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.ReadOnly, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Renders_Checkbox_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputCheckbox();
        var input = cut.Find("input[type=checkbox]");

        // Assert
        input.Should().NotBeNull();
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
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputCheckbox();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.SupportingText, "Helper text"));

        // Assert
        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Be("Helper text");
    }

    [Fact]
    public void ValidationMessage_Does_Not_Render_When_DisableValidationMessage_True() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = false;

        // Act
        var cut = RenderValidationInputCheckbox(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = false; // Invalid - required field

        // Act
        var cut = RenderValidationInputCheckbox(model);

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();
        cut.Instance.Should().BeOfType<TnTInputCheckbox>();
    }

    [Fact]
    public void Value_False_Renders_Unchecked_Checkbox() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = false;

        // Act
        var cut = RenderInputCheckbox(model);
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Value_True_Renders_Checked_Checkbox() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = true;

        // Act
        var cut = RenderInputCheckbox(model);
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("checked").Should().BeTrue();
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputCheckbox(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputCheckbox> RenderInputCheckbox(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputCheckbox>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputCheckbox>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputCheckbox> RenderValidationInputCheckbox(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputCheckbox>>? configure = null) {
        return Render<TnTInputCheckbox>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private class TestModel {
        public bool TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public bool TestValue { get; set; }
    }
}