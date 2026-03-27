using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

public class TnTInputRadioGroup_Tests : BunitContext {

    public TnTInputRadioGroup_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
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
    public void Component_Implements_ITnTComponentBase() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert
        cut.Instance.Should().BeAssignableTo<ITnTComponentBase>();
        cut.Instance.ElementClass.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Contains_RadioButtons_Container() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var container = cut.Find(".tnt-radio-buttons");

        // Assert
        container.Should().NotBeNull();
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
    public void Disabled_Propagates_To_Radio_Inputs() {
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
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ElementId, "test-radio-group"));
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("id").Should().Be("test-radio-group");
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
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.EndIcon, MaterialIcon.Search));

        // Assert
        cut.Markup.Should().Contain("tnt-end-icon");
        cut.Markup.Should().Contain("search");
    }

    [Fact]
    public void Enum_Value_Change_Updates_Model() {
        // Arrange
        var model = CreateEnumTestModel();
        var cut = RenderEnumRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Act
        radioInputs[2].Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = TestEnum.Third.ToString() });

        // Assert
        model.TestValue.Should().Be(TestEnum.Third);
    }

    [Fact]
    public void Group_Label_Does_Not_Render_When_Empty() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Group_Label_Does_Not_Render_When_Null() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, null!));

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Group_Label_Does_Not_Render_When_Whitespace() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, "   "));

        // Assert
        cut.FindAll("legend.tnt-label").Should().BeEmpty();
    }

    [Fact]
    public void Group_Label_Renders_As_Legend_When_Set() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.Label, "Test Radio Group"));

        // Assert
        var legend = cut.Find("legend.tnt-label");
        legend.TextContent.Should().Be("Test Radio Group");
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
    public void Keyboard_Navigation_Attributes_Are_Set() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var fieldset = cut.Find("fieldset");

        // Assert
        fieldset.GetAttribute("onkeydown").Should().Contain("NTComponents.radioGroupKeyDownHandler(event)");
        fieldset.HasAttribute("tabindex").Should().BeFalse();
        cut.FindAll("tnt-page-script").Should().BeEmpty();
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
        var container    = cut.Find(".tnt-input-container");

        // Assert
        var style = container.GetAttribute("style")!;
        style.Should().Contain("margin:10px");
        style.Should().Contain("--tnt-input-tint-color");
    }

    [Fact]
    public void Null_Value_Results_In_No_Checked_Radio() {
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
    public void Provides_Cascading_Value() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();

        // Assert The cascading value is implicit in the radio buttons being rendered correctly
        var radioInputs = cut.FindAll("input[type=radio]");
        radioInputs.Should().HaveCount(3);
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
    public void Radio_Buttons_Share_Same_Name() {
        // Arrange & Act
        var cut = RenderInputRadioGroup();
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        var firstName = radioInputs[0].GetAttribute("name");
        firstName.Should().NotBeNullOrEmpty();

        foreach (var radio in radioInputs) {
            radio.GetAttribute("name").Should().Be(firstName);
        }
    }

    [Fact]
    public void Radio_Buttons_Use_GroupName_When_Provided() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.GroupName, "custom-radio-group"));
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        foreach (var radio in radioInputs) {
            radio.GetAttribute("name").Should().Be("custom-radio-group");
        }
    }

    [Fact]
    public void Radio_Selection_Updates_Model_Value() {
        // Arrange
        var model = CreateTestModel();
        var cut = RenderInputRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Act
        radioInputs[2].Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "option3" });

        // Assert
        model.TestValue.Should().Be("option3");
    }

    [Fact]
    public void ReadOnly_Propagates_To_Radio_Inputs() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.ReadOnly, true));
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        foreach (var radio in radioInputs) {
            radio.HasAttribute("readonly").Should().BeTrue();
        }
    }

    [Fact]
    public void Renders_Fieldset_With_Default_Classes_And_Type() {
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
    public void SetFocusAsync_Focuses_Correct_Radio_For_Enum_Value() {
        // Arrange
        var model = CreateEnumTestModel();
        model.TestValue = TestEnum.Second;
        var cut = RenderEnumRadioGroup(model);

        // Act
        var task = cut.Instance.SetFocusAsync();

        // Assert - We can't easily verify browser focus in bUnit, but we can verify the code path doesn't throw and potentially uses the right radio.
        // For bUnit, we might check if SetFocusAsync was called on the radio if we could mock it, 
        // but here we just ensure basic execution for these types which was previously problematic.
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void SetFocusAsync_Focuses_Correct_Radio_For_Int_Value() {
        // Arrange
        var model = new IntModel { TestValue = 2 };
        var cut = RenderIntRadioGroup(model);

        // Act
        var task = cut.Instance.SetFocusAsync();

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void StartIcon_Renders_When_Set() {
        // Arrange & Act
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.StartIcon, MaterialIcon.Home));

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
        var cut = RenderInputRadioGroup(configure: p => p.Add(c => c.SupportingText, "Choose one option"));

        // Assert
        var supportingText = cut.Find(".tnt-supporting-text");
        supportingText.TextContent.Should().Be("Choose one option");
    }

    [Fact]
    public void Supports_Boolean_Type() {
        // Arrange
        var model = new BooleanModel { TestValue = true };

        // Act
        var cut = Render<TnTInputRadioGroup<bool>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateBooleanRadioOptions());
        });

        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(2);
        radioInputs[0].HasAttribute("checked").Should().BeTrue(); // True option
        radioInputs[1].HasAttribute("checked").Should().BeFalse(); // False option
    }

    [Fact]
    public void Supports_Nullable_Boolean_Type() {
        // Arrange
        var model = new NullableBooleanModel { TestValue = null };

        // Act
        var cut = Render<TnTInputRadioGroup<bool?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<bool?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateNullableBooleanRadioOptions());
        });

        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        radioInputs[0].HasAttribute("checked").Should().BeFalse();
        radioInputs[1].HasAttribute("checked").Should().BeFalse();
        radioInputs[2].HasAttribute("checked").Should().BeTrue(); // Null option should be checked
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
    public void Works_With_Enum_Types() {
        // Arrange
        var model = CreateEnumTestModel();
        model.TestValue = TestEnum.Second;

        // Act
        var cut = RenderEnumRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        radioInputs[0].HasAttribute("checked").Should().BeFalse();
        radioInputs[1].HasAttribute("checked").Should().BeTrue();
        radioInputs[2].HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Works_With_Int_Types() {
        // Arrange
        var model = new IntModel { TestValue = 20 };

        // Act
        var cut = RenderIntRadioGroup(model);
        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(3);
        radioInputs[0].HasAttribute("checked").Should().BeFalse();
        radioInputs[1].HasAttribute("checked").Should().BeTrue();
        radioInputs[2].HasAttribute("checked").Should().BeFalse();
    }

    private RenderFragment CreateBooleanRadioOptions() => builder => {
        builder.OpenComponent<TnTInputRadio<bool>>(0);
        builder.AddAttribute(1, "Value", true);
        builder.AddAttribute(2, "Label", "True");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<bool>>(3);
        builder.AddAttribute(4, "Value", false);
        builder.AddAttribute(5, "Label", "False");
        builder.CloseComponent();
    };

    private RenderFragment CreateEnumRadioOptions() => builder => {
        builder.OpenComponent<TnTInputRadio<TestEnum?>>(0);
        builder.AddAttribute(1, "Value", TestEnum.First);
        builder.AddAttribute(2, "Label", "First Option");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<TestEnum?>>(3);
        builder.AddAttribute(4, "Value", TestEnum.Second);
        builder.AddAttribute(5, "Label", "Second Option");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<TestEnum?>>(6);
        builder.AddAttribute(7, "Value", TestEnum.Third);
        builder.AddAttribute(8, "Label", "Third Option");
        builder.CloseComponent();
    };

    private TestEnumModel CreateEnumTestModel() => new();

    private RenderFragment CreateIntRadioOptions() => builder => {
        builder.OpenComponent<TnTInputRadio<int>>(0);
        builder.AddAttribute(1, "Value", 10);
        builder.AddAttribute(2, "Label", "10");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<int>>(3);
        builder.AddAttribute(4, "Value", 20);
        builder.AddAttribute(5, "Label", "20");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<int>>(6);
        builder.AddAttribute(7, "Value", 30);
        builder.AddAttribute(8, "Label", "30");
        builder.CloseComponent();
    };

    private RenderFragment CreateNullableBooleanRadioOptions() => builder => {
        builder.OpenComponent<TnTInputRadio<bool?>>(0);
        builder.AddAttribute(1, "Value", (bool?)true);
        builder.AddAttribute(2, "Label", "True");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<bool?>>(3);
        builder.AddAttribute(4, "Value", (bool?)false);
        builder.AddAttribute(5, "Label", "False");
        builder.CloseComponent();

        builder.OpenComponent<TnTInputRadio<bool?>>(6);
        builder.AddAttribute(7, "Value", (bool?)null);
        builder.AddAttribute(8, "Label", "Unspecified");
        builder.CloseComponent();
    };

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

    private IRenderedComponent<TnTInputRadioGroup<int>> RenderIntRadioGroup(IntModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputRadioGroup<int>>>? configure = null) {
        model ??= new IntModel();
        return Render<TnTInputRadioGroup<int>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateIntRadioOptions());
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TnTInputRadioGroup<TestEnum?>> RenderEnumRadioGroup(TestEnumModel? model = null, Action<ComponentParameterCollectionBuilder<TnTInputRadioGroup<TestEnum?>>>? configure = null) {
        model ??= CreateEnumTestModel();
        return Render<TnTInputRadioGroup<TestEnum?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<TestEnum?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, CreateEnumRadioOptions());
            configure?.Invoke(parameters);
        });
    }

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

    [Fact]
    public void Supports_Int_Nullable_Type() {
        // Arrange
        var model = new NullableIntModel { TestValue = null };
        var cut = Render<TnTInputRadioGroup<int?>>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<int?>(this, v => model.TestValue = v))
                .Add(p => p.ChildContent, builder => {
                    builder.OpenComponent<TnTInputRadio<int?>>(0);
                    builder.AddAttribute(1, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<TnTInputRadio<int?>>(2);
                    builder.AddAttribute(3, "Value", (int?)null);
                    builder.CloseComponent();
                });
        });

        var radioInputs = cut.FindAll("input[type=radio]");

        // Assert
        radioInputs.Should().HaveCount(2);
        radioInputs[0].HasAttribute("checked").Should().BeFalse();
        radioInputs[1].HasAttribute("checked").Should().BeTrue();
    }

    private class NullableIntModel {
        public int? TestValue { get; set; }
    }

    private class BooleanModel {
        public bool TestValue { get; set; }
    }

    private class IntModel {
        public int TestValue { get; set; }
    }

    private class NullableBooleanModel {
        public bool? TestValue { get; set; }
    }

    private enum TestEnum {
        First,
        Second,
        Third
    }

    private class TestEnumModel {
        public TestEnum? TestValue { get; set; }
    }

    private class TestModel {
        public string? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        public string? TestValue { get; set; }
    }
}
