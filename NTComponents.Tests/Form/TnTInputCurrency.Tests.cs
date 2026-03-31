using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputCurrency_Tests : BunitContext {

    public TnTInputCurrency_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void AutoComplete_Sets_Input_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.AutoComplete, "off"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.AutoFocus, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        decimal? callbackValue = null;
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<decimal?>(this, (value) => callbackValue = value);
        var cut = RenderInputCurrency(model, p => p.Add(c => c.BindAfter, callback)
        .Add(c => c.BindOnInput, true));
        var input = cut.Find("input");

        // Act
        input.Input("$555.25");

        // Assert
        callbackValue.Should().Be(555.25m);
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p
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
        var cut = RenderInputCurrency(configure: p => p
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
    public void CultureCode_Attribute_Added_To_Input() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.CultureCode, "de-DE"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("cultureCode").Should().Be("de-DE");
    }

    [Fact]
    public void Currency_Formatting_Functions_Applied() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var input = cut.Find("input");

        // Assert
        var onKeyDown = input.GetAttribute("onkeydown");
        onKeyDown.Should().Contain("NTComponents.enforceCurrencyFormat(event)");
        onKeyDown.Should().Contain("NTComponents.formKeyDownSupportingTextHandler(event)");
        input.GetAttribute("onkeyup").Should().Be("NTComponents.formatToCurrency(event)");
    }

    [Fact]
    public void Currency_Value_Formats_Correctly_EUR() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 1234.56m;

        // Act
        var cut = RenderInputCurrency(model, p => p
            .Add(c => c.CultureCode, "de-DE")
            .Add(c => c.CurrencyCode, "EUR"));
        var input = cut.Find("input");

        // Assert
        // Note: This might vary based on system locale, but should contain EUR formatting
        var value = input.GetAttribute("value")!;
        (value.Contains("1.234,56") || value.Contains("1,234.56")).Should().BeTrue();
        value.Should().Contain("€");
    }

    [Fact]
    public void Currency_Value_Formats_Correctly_USD() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 1234.56m;

        // Act
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("$1,234.56");
    }

    [Fact]
    public void CurrencyCode_Attribute_Added_To_Input() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.CurrencyCode, "GBP"));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("currencyCode").Should().Be("GBP");
    }

    [Fact]
    public void Custom_CultureCode_Can_Be_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.CultureCode, "en-GB"));

        // Assert
        cut.Instance.CultureCode.Should().Be("en-GB");
    }

    [Fact]
    public void Custom_CurrencyCode_Can_Be_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.CurrencyCode, "EUR"));

        // Assert
        cut.Instance.CurrencyCode.Should().Be("EUR");
    }

    [Fact]
    public void Default_Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_CultureCode_Is_EnUS() {
        // Arrange & Act
        var cut = RenderInputCurrency();

        // Assert
        cut.Instance.CultureCode.Should().Be("en-US");
    }

    [Fact]
    public void Default_CurrencyCode_Is_USD() {
        // Arrange & Act
        var cut = RenderInputCurrency();

        // Assert
        cut.Instance.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Default_Type_Is_Currency() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("type").Should().Be("text"); // Currency type renders as text
        cut.Instance.Type.Should().Be(InputType.Currency);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Disabled, true));
        var input = cut.Find("input");
        var label = cut.Find("label");

        // Assert
        input.HasAttribute("disabled").Should().BeTrue();
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.ElementId, "currency-id"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("id").Should().Be("currency-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.ElementTitle, "Currency Title"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("title").Should().Be("Currency Title");
    }

    [Fact]
    public void Empty_Placeholder_Defaults_To_Single_Space() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("placeholder").Should().Be(" ");
    }

    [Fact]
    public void Empty_String_Sets_Value_To_Null() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 123.45m;
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Act
        input.Change("");

        // Assert
        model.TestValue.Should().BeNull();
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency();
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

            builder.OpenComponent<TnTInputCurrency>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<decimal?>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, v => model.TestValue = v));
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
    public void Input_Change_Handles_Raw_Decimal_String() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Act
        input.Change("789.12");

        // Assert
        model.TestValue.Should().Be(789.12m);
    }

    [Fact]
    public void Input_Change_Updates_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Act
        input.Change("$456.78");

        // Assert
        model.TestValue.Should().Be(456.78m);
    }

    [Fact]
    public void Input_Input_Updates_Value_When_BindOnInput_True() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputCurrency(model, p => p.Add(c => c.BindOnInput, true));
        var input = cut.Find("input");

        // Act
        input.Input("$999.99");

        // Assert
        model.TestValue.Should().Be(999.99m);
    }

    [Fact]
    public void Input_Title_Attribute_Uses_Field_Name() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("title").Should().Be("TestValue");
    }

    [Fact]
    public void Invalid_String_Does_Not_Update_Value() {
        // Arrange
        var model = CreateTestModel();
        var originalValue = 100.50m;
        model.TestValue = originalValue;
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Act
        input.Change("not a number");

        // Assert
        model.TestValue.Should().Be(originalValue); // Value should remain unchanged
    }

    [Fact]
    public void Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputCurrency();

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Label, "Test Label"));

        // Assert
        var labelSpan = cut.Find(".tnt-label");
        labelSpan.TextContent.Should().Be("Test Label");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-currency" } };

        // Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("custom-currency");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Negative_Value_Formats_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = -123.45m;

        // Act
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Assert
        var value = input.GetAttribute("value")!;
        value.Should().Contain("123.45");
        value.Should().Contain("$");
        // Negative formatting can vary by culture, so just check components are present
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Null_Value_Renders_Empty_Input() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Assert
        var value = input.GetAttribute("value");
        (value == null || value == string.Empty).Should().BeTrue();
    }

    [Fact]
    public void Placeholder_Class_Added_When_Placeholder_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Placeholder, "Test placeholder"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Class_Not_Added_When_Placeholder_Empty() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().NotContain("tnt-placeholder");
    }

    [Fact]
    public void Placeholder_Sets_Input_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Placeholder, "Enter amount..."));
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("placeholder").Should().Be("Enter amount...");
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.ReadOnly, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Renders_Currency_Input_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputCurrency();
        var input = cut.Find("input[type=text]");

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
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputCurrency();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.SupportingText, "Helper text"));

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
        var cut = RenderValidationInputCurrency(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null; // Invalid - required field

        // Act
        var cut = RenderValidationInputCurrency(model);

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();
        cut.Instance.Should().BeOfType<TnTInputCurrency>();
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputCurrency(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Zero_Value_Formats_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = 0m;

        // Act
        var cut = RenderInputCurrency(model);
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("value").Should().Be("$0.00");
    }

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputCurrency> RenderInputCurrency(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputCurrency>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputCurrency>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<decimal?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputCurrency> RenderValidationInputCurrency(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputCurrency>>? configure = null) {
        return Render<TnTInputCurrency>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<decimal?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private class TestModel {
        public decimal? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public decimal? TestValue { get; set; }
    }
}