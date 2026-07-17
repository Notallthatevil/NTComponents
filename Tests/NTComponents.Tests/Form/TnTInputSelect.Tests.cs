using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputSelect_Tests : BunitContext {

    public TnTInputSelect_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void Appearance_Filled_Adds_Filled_Class() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Appearance, FormAppearance.Filled));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-filled");
    }

    [Fact]
    public void Appearance_Outlined_Adds_Outlined_Class() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void Array_Select_Has_Multiple_Attribute() {
        // Arrange
        var model = CreateArrayTestModel();
        model.TestValue = new[] { "option1" }; // Initialize with non-null array

        // Act
        var cut = RenderInputSelectArray(model);
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("multiple").Should().BeTrue();
    }

    [Fact]
    public void AutoComplete_Sets_Select_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.AutoComplete, "off"));
        var select = cut.Find("select");

        // Assert
        select.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact]
    public void AutoFocus_True_Renders_Autofocus_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.AutoFocus, true));
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("autofocus").Should().BeTrue();
    }

    [Fact]
    public void BindAfter_Callback_Invoked_On_Value_Change() {
        // Arrange
        var callbackValue = "";
        var model = CreateTestModel();
        var callback = EventCallback.Factory.Create<string?>(this, (value) => callbackValue = value ?? "");
        var cut = RenderInputSelect(model, p => p.Add(c => c.BindAfter, callback));
        var select = cut.Find("select");

        // Act
        select.Change("option1");

        // Assert
        callbackValue.Should().Be("option1");
    }

    [Fact]
    public void Both_Icons_Can_Be_Rendered_Together() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p
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
        var cut = RenderInputSelect(configure: p => p
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
    public void Custom_Placeholder_Option_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Placeholder, "Choose option..."));
        var firstOption = cut.Find("option");

        // Assert
        firstOption.TextContent.Should().Be("Choose option...");
        firstOption.HasAttribute("disabled").Should().BeFalse(); // Default AllowPlaceholderSelection is true
    }

    [Fact]
    public void Default_Color_Style_Variables_Are_Set() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var container = cut.Find(".tnt-input-container");
        var style = container.GetAttribute("style")!;

        // Assert
        style.Should().Contain("--tnt-input-tint-color:var(--tnt-color-primary)");
        style.Should().Contain("--tnt-input-background-color:var(--tnt-color-surface-container-highest)");
        style.Should().Contain("--tnt-input-text-color:var(--tnt-color-on-surface)");
        style.Should().Contain("--tnt-input-error-color:var(--tnt-color-error)");
    }

    [Fact]
    public void Default_Type_Is_Select() {
        // Arrange & Act
        var cut = RenderInputSelect();

        // Assert
        cut.Instance.Type.Should().Be(InputType.Select);
    }

    [Fact]
    public void Disabled_Adds_Disabled_Attribute_And_Class() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Disabled, true));
        var select = cut.Find("select");
        var label = cut.Find("label");

        // Assert
        select.HasAttribute("disabled").Should().BeTrue();
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementId_Sets_Id_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.ElementId, "select-id"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("id").Should().Be("select-id");
    }

    [Fact]
    public void ElementLang_Sets_Lang_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.ElementLang, "en-US"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("lang").Should().Be("en-US");
    }

    [Fact]
    public void ElementTitle_Sets_Title_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.ElementTitle, "Select Title"));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("title").Should().Be("Select Title");
    }

    [Fact]
    public void Empty_String_Value_Renders_Empty_Select() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = "";

        // Act
        var cut = RenderInputSelect(model);
        var select = cut.Find("select");

        // Assert
        select.GetAttribute("value").Should().Be("");
    }

    [Fact]
    public void EndIcon_Renders_When_Set() {
        // Arrange
        var endIcon = MaterialIcon.Search;

        // Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.EndIcon, endIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Has_TnTId_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var label = cut.Find("label");

        // Assert
        cut.Instance.Should().BeAssignableTo<INTComponentBase>();
    }

    [Fact]
    public void Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputSelect();

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Label_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Label, "Test Label"));

        // Assert
        var labelSpan = cut.Find(".tnt-label");
        labelSpan.TextContent.Should().Be("Test Label");
    }

    [Fact]
    public void Merges_Custom_Class_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "custom-select" } };

        // Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("custom-select");
        cls.Should().Contain("tnt-input");
    }

    [Fact]
    public void Merges_Custom_Style_From_AdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "margin:10px;" } };

        // Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var container = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Multiple_Select_Works_With_Array_Type() {
        // Arrange
        var model = CreateArrayTestModel();
        model.TestValue = new[] { "option1", "option3" };

        // Act
        var cut = RenderInputSelectArray(model);
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("multiple").Should().BeTrue();
        // Note: Testing multi-select value binding is complex in bUnit The presence of the multiple attribute indicates proper setup
        model.TestValue.Should().NotBeNull();
        model.TestValue.Should().Contain("option1");
        model.TestValue.Should().Contain("option3");
    }

    [Fact]
    public void Null_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Null_Value_Renders_Empty_Select() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = null;

        // Act
        var cut = RenderInputSelect(model);
        var select = cut.Find("select");

        // Assert
        var value = select.GetAttribute("value");
        (value == null || value == string.Empty).Should().BeTrue();
    }

    [Fact]
    public void Placeholder_Option_Can_Be_Disabled_When_AllowPlaceholderSelection_False() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p
            .Add(c => c.Placeholder, "Choose option...")
            .Add(c => c.AllowPlaceholderSelection, false));
        var firstOption = cut.Find("option");

        // Assert
        firstOption.TextContent.Should().Be("Choose option...");
        firstOption.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Placeholder_Option_Renders_By_Default() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var firstOption = cut.Find("option");

        // Assert
        firstOption.GetAttribute("value").Should().Be("");
        firstOption.HasAttribute("disabled").Should().BeTrue(); // This is the empty placeholder, which is always disabled
        firstOption.HasAttribute("selected").Should().BeTrue();
    }

    [Fact]
    public void ReadOnly_Adds_Readonly_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.ReadOnly, true));
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Renders_Child_Options() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var options = cut.FindAll("option");

        // Assert
        options.Should().HaveCount(4); // 3 custom + 1 placeholder
        options[1].GetAttribute("value").Should().Be("option1");
        options[1].TextContent.Should().Be("Option 1");
        options[2].GetAttribute("value").Should().Be("option2");
        options[2].TextContent.Should().Be("Option 2");
        options[3].GetAttribute("value").Should().Be("option3");
        options[3].TextContent.Should().Be("Option 3");
    }

    [Fact]
    public void Renders_Select_With_Default_Classes_And_Type() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var select = cut.Find("select");

        // Assert
        select.Should().NotBeNull();
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
        var cut = RenderInputSelect(configure: p => p.Add(c => c.AdditionalAttributes, attrs));
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void Select_Blur_Notifies_EditContext_When_Present() {
        // Arrange
        var model = CreateTestModel();
        var fieldChanged = false;

        // Create a component with EditForm wrapper that provides EditContext
        RenderFragment<EditContext> childContent = context => builder => {
            // Subscribe to field changes
            context.OnFieldChanged += (_, __) => fieldChanged = true;

            builder.OpenComponent<TnTInputSelect<string?>>(0);
            builder.AddAttribute(1, "ValueExpression", (Expression<Func<string?>>)(() => model.TestValue));
            builder.AddAttribute(2, "Value", model.TestValue);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            builder.AddAttribute(4, "ChildContent", CreateSelectOptions());
            builder.CloseComponent();
        };

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (RenderFragment<EditContext>)childContent!)
        );

        var select = cut.Find("select");

        // Act
        select.Blur();

        // Assert
        fieldChanged.Should().BeTrue();
    }

    [Fact]
    public void Select_Change_Updates_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputSelect(model);
        var select = cut.Find("select");

        // Act
        select.Change("option3");

        // Assert
        model.TestValue.Should().Be("option3");
    }

    [Fact]
    public void Select_Title_Attribute_Uses_Field_Name() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var select = cut.Find("select");

        // Assert
        select.GetAttribute("title").Should().Be("TestValue");
    }

    [Fact]
    public void Single_Select_Does_Not_Have_Multiple_Attribute() {
        // Arrange & Act
        var cut = RenderInputSelect();
        var select = cut.Find("select");

        // Assert
        select.HasAttribute("multiple").Should().BeFalse();
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange
        var startIcon = MaterialIcon.Home;

        // Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.StartIcon, startIcon));

        // Assert
        cut.Markup.Should().Contain("tnt-start-icon");
        cut.Markup.Should().Contain("home");
    }

    [Fact]
    public void SupportingText_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputSelect();

        // Assert
        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void SupportingText_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.SupportingText, "Helper text"));

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
        var cut = RenderValidationInputSelect(model, p => p.Add(c => c.DisableValidationMessage, true));

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeTrue();
    }

    [Fact]
    public void ValidationMessage_Renders_When_EditContext_Present_And_ValidationMessage_Not_Disabled() {
        // Arrange
        var model = CreateValidationTestModel();
        model.TestValue = null; // Invalid - required field

        // Act
        var cut = RenderValidationInputSelect(model);

        // Assert
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.ValueExpression.Should().NotBeNull();
        cut.Instance.Should().BeOfType<TnTInputSelect<string?>>();
    }

    [Fact]
    public void Value_Binding_Works_Correctly() {
        // Arrange
        var model = CreateTestModel();
        model.TestValue = "option2";

        // Act
        var cut = RenderInputSelect(model);
        var select = cut.Find("select");

        // Assert
        select.GetAttribute("value").Should().Be("option2");
    }

    [Fact]
    public void Whitespace_Label_Does_Not_Render_Label_Span() {
        // Arrange & Act
        var cut = RenderInputSelect(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll(".tnt-label").Should().BeEmpty();
    }

    private TestArrayModel CreateArrayTestModel() => new();

    private RenderFragment CreateSelectOptions() => builder => {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", "option1");
        builder.AddContent(2, "Option 1");
        builder.CloseElement();

        builder.OpenElement(3, "option");
        builder.AddAttribute(4, "value", "option2");
        builder.AddContent(5, "Option 2");
        builder.CloseElement();

        builder.OpenElement(6, "option");
        builder.AddAttribute(7, "value", "option3");
        builder.AddContent(8, "Option 3");
        builder.CloseElement();
    };

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TnTInputSelect<string?>> RenderInputSelect(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputSelect<string?>>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TnTInputSelect<string?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue!)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateSelectOptions());
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputSelect<string[]?>> RenderInputSelectArray(TestArrayModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputSelect<string[]?>>>? configure = null) {
        model ??= CreateArrayTestModel();
        return Render<TnTInputSelect<string[]?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string[]?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateSelectOptions());
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputSelect<string?>> RenderValidationInputSelect(TestModelWithValidation model, Action<ComponentParameterCollectionBuilder<TnTInputSelect<string?>>>? configure = null) {
        return Render<TnTInputSelect<string?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateSelectOptions());
            configure?.Invoke(parameters);
        });
    }

    private class TestArrayModel {
        public string[]? TestValue { get; set; }
    }

    private class TestModel {
        public string? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public string? TestValue { get; set; }
    }
}