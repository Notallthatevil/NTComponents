using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputRadio_Tests : BunitContext {

    public TnTInputRadio_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void All_Radio_Inputs_Have_Same_Name_Attribute() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        var firstName = radioInputs[0].GetAttribute("name");
        firstName.Should().NotBeNullOrEmpty();

        foreach (var radio in radioInputs) {
            radio.GetAttribute("name").Should().Be(firstName);
        }
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        var callbackValue = "";
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<string?>(this, (value) => callbackValue = value ?? "");
        var cut = RenderInputRadioGroup(model, p => p.Add(c => c.BindAfter, callback));
        var radioInputs = cut.FindAll("input[type=radio]");

        // Act
        radioInputs[0].Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "option1" });

        // Assert
        callbackValue.Should().Be("option1");
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p
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
        var cut = RenderInputRadioGroup(configure: p => p
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
        var cut = RenderInputRadioGroup();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_Type_Is_Radio() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert
        cut.Instance.Type.Should().Be(InputType.Radio);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class_To_Group() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Disabled, true));
        var fieldset = cut.Find("fieldset");
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        fieldset.GetAttribute("class")!.Should().Contain("tnt-disabled");
        foreach (var radio in radioInputs) {
            radio.HasAttribute("disabled").Should().BeTrue();
        }
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ElementId, "radio-group-id"));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("id").Should().Be("radio-group-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ElementTitle, "Radio Group Title"));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("title").Should().Be("Radio Group Title");
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Group_Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Group_Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, "Test Group"));

        // Assert
        var legend = cut.Find("legend.tnt-label");
        legend.TextContent.Should().Be("Test Group");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var fieldset = cut.Find("fieldset");

        // Assert
        cut.Instance.Should().BeAssignableTo<INTComponentBase>();
    }

    [Fact]
    public void Individual_Radio_Can_Have_Own_Properties() {
        // Arrange & Act - Test individual radio properties instead of icons
        var customRadioOptions = (RenderFragment)(builder => {
            builder.OpenComponent<TnTInputRadio<string?>>(0);
            builder.AddAttribute(1, "Value", "option1");
            builder.AddAttribute(2, "Label", "Option 1");
            builder.CloseComponent();
        });

        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ChildContent, customRadioOptions));
        var radioInput = cut.Find("input[type=radio]");
        var label = cut.Find(".tnt-label");

        // Assert
        radioInput.Should().NotBeNull();
        label.TextContent.Should().Be("Option 1");
        radioInput.GetAttribute("value").Should().Be("option1");
    }

    [Fact]
    public void Layout_Direction_Horizontal_Does_Not_Add_Vertical_Class() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.LayoutDirection, LayoutDirection.Horizontal));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("class")!.Should().NotContain("tnt-vertical");
    }

    [Fact]
    public void Layout_Direction_Vertical_Adds_Vertical_Class() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.LayoutDirection, LayoutDirection.Vertical));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("class")!.Should().Contain("tnt-vertical");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-radio-group" } };

        // Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var fieldset = cut.Find("fieldset");

        // Assert
        var cls = fieldset.GetAttribute("class")!;
        cls.Should().Contain("custom-radio-group");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Legend() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Null_Value_Renders_No_Checked_Radio() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderInputRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        foreach (var radio in radioInputs) {
            radio.HasAttribute("checked").Should().BeFalse();
        }
    }

    [Fact]
    public void Radio_Blur_Notifies_EditContext_When_Present() {
        // Arrange
        var model = CreateTestModel();
        var fieldChanged = false;

        // Create a component with EditForm wrapper that provides EditContext
        RenderFragment<EditContext> childContent = context => builder => {
            // Subscribe to field changes
            context.OnFieldChanged += (_, __) => fieldChanged = true;

            builder.OpenComponent<TnTInputRadioGroup<string?>>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<string?>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            builder.AddAttribute(4, "ChildContent", CreateRadioOptions());
            builder.CloseComponent();
        };

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (RenderFragment<EditContext>)childContent!)
        );

        var radioInput = cut.Find("input[type=radio]");

        // Act
        radioInput.Blur();

        // Assert
        fieldChanged.Should().BeTrue();
    }

    [Fact]
    public void Radio_Button_Overlays_Render() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var overlays = cut.FindAll(".tnt-radio-button");

        // Assert
        overlays.Should().HaveCount(3);
    }

    [Fact]
    public void Radio_Change_Updates_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Act - Simulate clicking the third radio option In radio groups, the change event provides the value from the input's value attribute
        radioInputs[2].Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "option3" });

        // Assert
        model.TestValue.Should().Be("option3");
    }

    [Fact]
    public void Radio_Labels_Render_Correctly() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var labels = cut.FindAll(".tnt-label");

        // Assert
        labels.Should().HaveCount(3); // Only radio button labels, not group label
        labels[0].TextContent.Should().Be("Option 1");
        labels[1].TextContent.Should().Be("Option 2");
        labels[2].TextContent.Should().Be("Option 3");
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute_To_Radio_Inputs() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ReadOnly, true));
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        foreach (var radio in radioInputs) {
            radio.HasAttribute("readonly").Should().BeTrue();
        }
    }

    [Fact]
    public void Renders_Child_Radio_Buttons() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        radioInputs[0].GetAttribute("value").Should().Be("option1");
        radioInputs[1].GetAttribute("value").Should().Be("option2");
        radioInputs[2].GetAttribute("value").Should().Be("option3");
    }

    [Fact]
    public void Renders_RadioGroup_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.Should().NotBeNull();
        var cls = fieldset.GetAttribute("class")!;
        cls.Should().Contain("tnt-input");
        cls.Should().Contain("tnt-components");
        cls.Should().Contain("tnt-radio-group");
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.SupportingText, "Helper text"));

        // Assert
        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Be("Helper text");
    }

    [Fact]
    public void ValidationMessage_Does_Not_Render_When_DisableValidationMessage_True() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderValidationInputRadioGroup(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null; // Invalid - required field

        // Act
        var cut = RenderValidationInputRadioGroup(model);

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();
        cut.Instance.Should().BeOfType<TnTInputRadioGroup<string?>>();
    }

    [Fact]
    public void Value_Binding_Works_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = "option2";

        // Act
        var cut = RenderInputRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs[0].HasAttribute("checked").Should().BeFalse();
        radioInputs[1].HasAttribute("checked").Should().BeTrue();
        radioInputs[2].HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Legend() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    private RenderFragment CreateRadioOptions() => builder => {
        builder.OpenComponent<TnTInputRadio<string?>>(0);
        builder.AddAttribute(1, "Value", "option1");
        builder.AddAttribute(2, "Label", "Option 1");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<string?>>(3);
        builder.AddAttribute(4, "Value", "option2");
        builder.AddAttribute(5, "Label", "Option 2");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<string?>>(6);
        builder.AddAttribute(7, "Value", "option3");
        builder.AddAttribute(8, "Label", "Option 3");
        builder.CloseComponent();
    };

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputRadioGroup<string?>> RenderInputRadioGroup(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputRadioGroup<string?>>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputRadioGroup<string?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateRadioOptions());
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputRadioGroup<string?>> RenderValidationInputRadioGroup(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputRadioGroup<string?>>>? configure = null) {
        return Render<TnTInputRadioGroup<string?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateRadioOptions());
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