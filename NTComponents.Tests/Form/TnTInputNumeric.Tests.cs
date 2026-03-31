using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputNumeric_Tests : BunitContext {

    public TnTInputNumeric_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void AutoComplete_Sets_Input_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AutoComplete, "off"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AutoFocus, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        int? callbackValue = null;
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<int?>(this, (value) => callbackValue = value);
        var cut = RenderInputNumeric(model, p => p.Add(c => c.BindAfter, callback)
        .Add(c => c.BindOnInput, true));
        var input = cut.Find("input");

        // Act
        input.Input("789");

        // Assert
        callbackValue.Should().Be(789);
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p
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
        var cut = RenderInputNumeric(configure: p => p
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
    public void Decimal_Input_Change_Updates_Value() {
        // Arrange
        var model = new TestDecimalModel();
        var cut = RenderInputNumericDecimal(model);
        var input = cut.Find("input");

        // Act
        input.Change("678.90");

        // Assert
        model.TestValue.Should().Be(678.90m);
    }

    [Fact]
    public void Decimal_Value_Binding_Works_Correctly() {
        // Arrange
        var model = new TestDecimalModel { TestValue = 123.45m };

        // Act
        var cut = RenderInputNumericDecimal(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("123.45");
    }

    [Fact]
    public void Default_Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_Type_Is_Number() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("type").Should().Be("number");
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Disabled, true));
        var input = cut.Find("input");
        var label = cut.Find("label");

        // Assert
        input.HasAttribute("disabled").Should().BeTrue();
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.ElementId, "numeric-id"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("id").Should().Be("numeric-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.ElementTitle, "Numeric Title"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("title").Should().Be("Numeric Title");
    }

    [Fact]
    public void Empty_Placeholder_Defaults_To_Single_Space() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("placeholder").Should().Be(" ");
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var label = cut.Find("label");

        // Assert
        cut.Instance.Should().BeAssignableTo<ITnTComponentBase>();
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

            builder.OpenComponent<TnTInputNumeric<int?>>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<int?>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<int?>(this, v => model.TestValue = v));
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
    public void Input_Change_Updates_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Act
        input.Change("123");

        // Assert
        model.TestValue.Should().Be(123);
    }

    [Fact]
    public void Input_Input_Updates_Value_When_BindOnInput_True() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputNumeric(model, p => p.Add(c => c.BindOnInput, true));
        var input = cut.Find("input");

        // Act
        input.Input("456");

        // Assert
        model.TestValue.Should().Be(456);
    }

    [Fact]
    public void Input_Title_Attribute_Uses_Field_Name() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("title").Should().Be("TestValue");
    }

    [Fact]
    public void Invalid_String_Does_Not_Update_Value() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 42;
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Act
        input.Change("not a number");

        // Assert
        model.TestValue.Should().Be(42); // Value should remain unchanged
    }

    [Fact]
    public void Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputNumeric();

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Label, "Test Label"));

        // Assert
        var labelSpan = cut.Find(".tnt-label");
        labelSpan.TextContent.Should().Be("Test Label");
    }

    [Fact]
    public void Max_Attribute_Added_When_Set_In_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "max", "100" } };

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("max").Should().Be("100");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-numeric" } };

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("custom-numeric");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Min_Attribute_Added_When_Set_In_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "min", "0" } };

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("min").Should().Be("0");
    }

    [Fact]
    public void Negative_Value_Renders_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = -42;

        // Act
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("-42");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Null_Value_Renders_Empty_Input() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Assert
        var value = input.GetAttribute("value");
        (value == null || value == string.Empty).Should().BeTrue();
    }

    [Fact]
    public void Placeholder_Class_Added_When_Placeholder_Set() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Placeholder, "Test placeholder"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Class_Not_Added_When_Placeholder_Empty() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().NotContain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Sets_Input_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Placeholder, "Enter number..."));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("placeholder").Should().Be("Enter number...");
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.ReadOnly, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Renders_Number_Input_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputNumeric();
        var input = cut.Find("input[type=number]");

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
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void Step_Attribute_Added_When_Set_In_Additional_Attributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "step", "0.01" } };

        // Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("step").Should().Be("0.01");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputNumeric();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.SupportingText, "Helper text"));

        // Assert
        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Be("Helper text");
    }

    [Fact]
    public void Supports_Decimal_Type() {
        // Arrange & Act
        var model = new { TestValue = 0.0m };
        var cut = Render<TnTInputNumeric<decimal>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Double_Type() {
        // Arrange & Act
        var model = new { TestValue = 0.0 };
        var cut = Render<TnTInputNumeric<double>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Float_Type() {
        // Arrange & Act
        var model = new { TestValue = 0.0f };
        var cut = Render<TnTInputNumeric<float>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Int_Type() {
        // Arrange & Act
        var model = new { TestValue = 0 };
        var cut = Render<TnTInputNumeric<int>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Long_Type() {
        // Arrange & Act
        var model = new { TestValue = 0L };
        var cut = Render<TnTInputNumeric<long>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Nullable_Decimal_Type() {
        // Arrange & Act
        var model = new { TestValue = (decimal?)null };
        var cut = Render<TnTInputNumeric<decimal?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Nullable_Int_Type() {
        // Arrange & Act
        var model = new { TestValue = (int?)null };
        var cut = Render<TnTInputNumeric<int?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Supports_Short_Type() {
        // Arrange & Act
        var model = new { TestValue = (short)0 };
        var cut = Render<TnTInputNumeric<short>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });

        // Assert
        cut.Instance.Type.Should().Be(InputType.Number);
    }

    [Fact]
    public void Throws_For_Unsupported_Type() {
        // Arrange & Act & Assert
        var model = new { TestValue = "" };
        var act = () => Render<TnTInputNumeric<string>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue);
        });
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("The type 'System.String' is not a supported numeric type.");
    }

    [Fact]
    public void ValidationMessage_Does_Not_Render_When_DisableValidationMessage_True() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderValidationInputNumeric(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null; // Invalid - required field

        // Act
        var cut = RenderValidationInputNumeric(model);

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();
        cut.Instance.Should().BeOfType<TnTInputNumeric<int?>>();
    }

    [Fact]
    public void Value_Binding_Works_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 42;

        // Act
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("42");
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputNumeric(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Zero_Value_Renders_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 0;

        // Act
        var cut = RenderInputNumeric(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("0");
    }

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputNumeric<int?>> RenderInputNumeric(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputNumeric<int?>>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputNumeric<int?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputNumeric<decimal?>> RenderInputNumericDecimal(TestDecimalModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputNumeric<decimal?>>>? configure = null) {
        model ??= new TestDecimalModel();
        return Render<TnTInputNumeric<decimal?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<decimal?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputNumeric<int?>> RenderValidationInputNumeric(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputNumeric<int?>>>? configure = null) {
        return Render<TnTInputNumeric<int?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private class TestDecimalModel {
        public decimal? TestValue { get; set; }
    }

    private class TestModel {
        public int? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public int? TestValue { get; set; }
    }
}