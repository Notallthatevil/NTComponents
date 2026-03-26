using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Form;

/// <summary>
///     Unit tests for <see cref="TnTInputBase{TInputType}" />.
/// </summary>
public class TnTInputBase_Tests : BunitContext {

    public TnTInputBase_Tests() {
        // Set renderer info for tests that use NET9_0_OR_GREATER features
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void BothFormAndLocalStates_CanBeIndependentlyModified() {
        // Arrange
        var form = new MockTnTForm();

        // Test various combinations
        form.Disabled = false;
        var instance1 = RenderTestInputWithForm(form, disabled: false);
        instance1.FieldDisabled.Should().BeFalse();

        form.Disabled = true;
        var instance2 = RenderTestInputWithForm(form, disabled: false);
        instance2.FieldDisabled.Should().BeTrue(); // Form overrides

        form.Disabled = false;
        var instance3 = RenderTestInputWithForm(form, disabled: true);
        instance3.FieldDisabled.Should().BeTrue(); // Local takes effect

        form.ReadOnly = false;
        var instance4 = RenderTestInputWithForm(form, readOnly: false);
        instance4.FieldReadonly.Should().BeFalse();

        form.ReadOnly = true;
        var instance5 = RenderTestInputWithForm(form, readOnly: false);
        instance5.FieldReadonly.Should().BeTrue(); // Form overrides

        form.ReadOnly = false;
        var instance6 = RenderTestInputWithForm(form, readOnly: true);
        instance6.FieldReadonly.Should().BeTrue(); // Local takes effect
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert
        cut.Should().NotBeNull();
        cut.Instance.Should().NotBeNull();
        cut.Instance.Should().BeAssignableTo<ITnTComponentBase>();
        cut.Instance.Type.Should().Be(InputType.Text);
    }

    [Fact]
    public void DefaultParameters_HaveCorrectValues() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert
        cut.Instance.BackgroundColor.Should().Be(TnTColor.SurfaceContainerHighest);
        cut.Instance.TextColor.Should().Be(TnTColor.OnSurface);
        cut.Instance.TintColor.Should().Be(TnTColor.Primary);
        cut.Instance.ErrorColor.Should().Be(TnTColor.Error);
        cut.Instance.Disabled.Should().BeFalse();
        cut.Instance.ReadOnly.Should().BeFalse();
        cut.Instance.DisableValidationMessage.Should().BeFalse();
        cut.Instance.BindOnInput.Should().BeFalse();
    }

    [Fact]
    public void ElementClass_ContainsRequiredCssClasses() {
        // Arrange & Act
        var cut = RenderTestInput();
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("tnt-input");
        cls.Should().Contain("tnt-components");
    }

    [Fact]
    public void ElementClass_DoesNotIncludeDisabledClass_WhenFieldNotDisabled() {
        // Arrange & Act
        var cut = RenderTestInput();
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().NotContain("tnt-disabled");
    }

    [Fact]
    public void ElementClass_FormAppearanceOverridesLocal() {
        // Arrange
        var form = new MockTnTForm { Appearance = FormAppearance.Outlined };
        var model = CreateTestModel();

        // Act - Create component with filled appearance but form has outlined
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.AddAttribute(4, nameof(TestTnTInputBase.Appearance), FormAppearance.Filled); // Local appearance
                builder.CloseComponent();
            }));
        });
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("tnt-form-outlined"); // Form appearance takes precedence
        cls.Should().NotContain("tnt-form-filled");
    }

    [Fact]
    public void ElementClass_IncludesDisabledClass_WhenFieldDisabled() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, true));
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementClass_IncludesDisabledClass_WhenFormDisabled() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.CloseComponent();
            }));
        });
        var label = cut.Find("label");

        // Assert
        label.GetAttribute("class")!.Should().Contain("tnt-disabled");
    }

    [Fact]
    public void ElementClass_UsesFormAppearance_WhenFormProvided() {
        // Arrange
        var form = new MockTnTForm { Appearance = FormAppearance.Outlined };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.CloseComponent();
            }));
        });
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("tnt-form-outlined");
        cls.Should().NotContain("tnt-form-filled");
    }

    [Fact]
    public void ElementClass_UsesLocalAppearance_WhenNoFormProvided() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Appearance, FormAppearance.Outlined));
        var label = cut.Find("label");

        // Assert
        var cls = label.GetAttribute("class")!;
        cls.Should().Contain("tnt-form-outlined");
    }

    [Fact]
    public void ElementName_ReturnsNameAttributeValue() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert
        cut.Instance.ElementName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EnableRipple_AlwaysReturnsFalse() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert
        cut.Instance.EnableRipple.Should().BeFalse();
    }

    [Fact]
    public void FieldDisabled_HandlesNullForm_Gracefully() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert - Should not throw and should use local disabled state
        cut.Instance.FieldDisabled.Should().BeFalse();
    }

    [Fact]
    public void FieldDisabled_ReflectsCurrentFormState() {
        // Arrange
        var form = new MockTnTForm { Disabled = false };
        var instance = RenderTestInputWithForm(form);

        // Initial state
        instance.FieldDisabled.Should().BeFalse();

        // Act - Change form state (in real scenarios this would trigger re-render)
        form.Disabled = true;

        // Assert - The property should reflect the current form state
        // Note: In actual component usage, this would work through cascading parameters and re-rendering
        instance.FieldDisabled.Should().BeTrue();
    }

    [Fact]
    public void FieldDisabled_WhenBothFormAndLocalDisabledTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };

        // Act
        var instance = RenderTestInputWithForm(form, disabled: true);

        // Assert
        instance.FieldDisabled.Should().BeTrue();
    }

    [Fact]
    public void FieldDisabled_WhenFormDisabledFalse_ReturnsFalse() {
        // Arrange
        var form = new MockTnTForm { Disabled = false };

        // Act
        var instance = RenderTestInputWithForm(form);

        // Assert
        instance.FieldDisabled.Should().BeFalse();
    }

    [Fact]
    public void FieldDisabled_WhenFormDisabledFalseAndLocalDisabledTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { Disabled = false };

        // Act
        var instance = RenderTestInputWithForm(form, disabled: true);

        // Assert
        instance.FieldDisabled.Should().BeTrue(); // Local disabled takes effect
        instance.Disabled.Should().BeTrue();
    }

    [Fact]
    public void FieldDisabled_WhenFormDisabledTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };

        // Act
        var instance = RenderTestInputWithForm(form);

        // Assert
        instance.FieldDisabled.Should().BeTrue();
        instance.Disabled.Should().BeFalse(); // Local property should still be false
    }

    [Fact]
    public void FieldDisabled_WhenFormDisabledTrueAndLocalDisabledFalse_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };

        // Act
        var instance = RenderTestInputWithForm(form, disabled: false);

        // Assert
        instance.FieldDisabled.Should().BeTrue(); // Form takes precedence
        instance.Disabled.Should().BeFalse(); // Local property remains false
    }

    [Fact]
    public void FieldDisabled_WhenLocalDisabledFalse_ReturnsFalse() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, false));

        // Assert
        cut.Instance.FieldDisabled.Should().BeFalse();
        cut.Instance.Disabled.Should().BeFalse();
    }

    [Fact]
    public void FieldDisabled_WhenLocalDisabledTrue_ReturnsTrue() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, true));

        // Assert
        cut.Instance.FieldDisabled.Should().BeTrue();
        cut.Instance.Disabled.Should().BeTrue();
    }

    [Fact]
    public void FieldDisabled_WhenNoFormProvided_UsesLocalDisabledProperty() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, true));

        // Assert
        cut.Instance.FieldDisabled.Should().BeTrue();
    }

    [Fact]
    public void FieldReadonly_HandlesNullForm_Gracefully() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert - Should not throw and should use local readonly state
        cut.Instance.FieldReadonly.Should().BeFalse();
    }

    [Fact]
    public void FieldReadonly_ReflectsCurrentFormState() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = false };
        var instance = RenderTestInputWithForm(form);

        // Initial state
        instance.FieldReadonly.Should().BeFalse();

        // Act - Change form state (in real scenarios this would trigger re-render)
        form.ReadOnly = true;

        // Assert - The property should reflect the current form state
        // Note: In actual component usage, this would work through cascading parameters and re-rendering
        instance.FieldReadonly.Should().BeTrue();
    }

    [Fact]
    public void FieldReadonly_WhenBothFormAndLocalReadOnlyTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = true };

        // Act
        var instance = RenderTestInputWithForm(form, readOnly: true);

        // Assert
        instance.FieldReadonly.Should().BeTrue();
    }

    [Fact]
    public void FieldReadonly_WhenFormReadOnlyFalse_ReturnsFalse() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = false };

        // Act
        var instance = RenderTestInputWithForm(form);

        // Assert
        instance.FieldReadonly.Should().BeFalse();
    }

    [Fact]
    public void FieldReadonly_WhenFormReadOnlyFalseAndLocalReadOnlyTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = false };

        // Act
        var instance = RenderTestInputWithForm(form, readOnly: true);

        // Assert
        instance.FieldReadonly.Should().BeTrue(); // Local readonly takes effect
        instance.ReadOnly.Should().BeTrue();
    }

    [Fact]
    public void FieldReadonly_WhenFormReadOnlyTrue_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = true };

        // Act
        var instance = RenderTestInputWithForm(form);

        // Assert
        instance.FieldReadonly.Should().BeTrue();
        instance.ReadOnly.Should().BeFalse(); // Local property should still be false
    }

    [Fact]
    public void FieldReadonly_WhenFormReadOnlyTrueAndLocalReadOnlyFalse_ReturnsTrue() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = true };

        // Act
        var instance = RenderTestInputWithForm(form, readOnly: false);

        // Assert
        instance.FieldReadonly.Should().BeTrue(); // Form takes precedence
        instance.ReadOnly.Should().BeFalse(); // Local property remains false
    }

    [Fact]
    public void FieldReadonly_WhenLocalReadOnlyFalse_ReturnsFalse() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.ReadOnly, false));

        // Assert
        cut.Instance.FieldReadonly.Should().BeFalse();
        cut.Instance.ReadOnly.Should().BeFalse();
    }

    [Fact]
    public void FieldReadonly_WhenLocalReadOnlyTrue_ReturnsTrue() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.ReadOnly, true));

        // Assert
        cut.Instance.FieldReadonly.Should().BeTrue();
        cut.Instance.ReadOnly.Should().BeTrue();
    }

    [Fact]
    public void FieldReadonly_WhenNoFormProvided_UsesLocalReadOnlyProperty() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.ReadOnly, true));

        // Assert
        cut.Instance.FieldReadonly.Should().BeTrue();
    }

    [Fact]
    public void Input_DisabledAttribute_DoesNotRenderWhenFieldDisabledFalse() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, false));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Input_DisabledAttribute_RendersWhenFieldDisabledTrue() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.Disabled, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Input_DisabledAttribute_RendersWhenFormDisabled() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.CloseComponent();
            }));
        });
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Input_ReadonlyAttribute_DoesNotRenderWhenFieldReadonlyFalse() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.ReadOnly, false));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeFalse();
    }

    [Fact]
    public void Input_ReadonlyAttribute_RendersWhenFieldReadonlyTrue() {
        // Arrange & Act
        var cut = RenderTestInput(configure: p => p.Add(c => c.ReadOnly, true));
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Input_ReadonlyAttribute_RendersWhenFormReadOnly() {
        // Arrange
        var form = new MockTnTForm { ReadOnly = true };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.CloseComponent();
            }));
        });
        var input = cut.Find("input");

        // Assert
        input.HasAttribute("readonly").Should().BeTrue();
    }

    [Fact]
    public void Input_RendersWithCorrectType() {
        // Arrange & Act
        var cut = RenderTestInput();
        var input = cut.Find("input");

        // Assert
        input.GetAttribute("type").Should().Be("text");
    }

    [Fact]
    public void LogicalOR_Behavior_IsCorrect_ForDisabled() {
        // Test truth table for OR logic: (form.Disabled || local.Disabled)
        var testCases = new[]
        {
            new { Form = false, Local = false, Expected = false },
            new { Form = false, Local = true, Expected = true },
            new { Form = true, Local = false, Expected = true },
            new { Form = true, Local = true, Expected = true }
        };

        foreach (var testCase in testCases) {
            // Arrange
            var form = new MockTnTForm { Disabled = testCase.Form };
            var instance = RenderTestInputWithForm(form, disabled: testCase.Local);

            // Assert
            instance.FieldDisabled.Should().Be(testCase.Expected,
                $"Form.Disabled={testCase.Form}, Local.Disabled={testCase.Local} should result in FieldDisabled={testCase.Expected}");
        }
    }

    [Fact]
    public void LogicalOR_Behavior_IsCorrect_ForReadonly() {
        // Test truth table for OR logic: (form.ReadOnly || local.ReadOnly)
        var testCases = new[]
        {
            new { Form = false, Local = false, Expected = false },
            new { Form = false, Local = true, Expected = true },
            new { Form = true, Local = false, Expected = true },
            new { Form = true, Local = true, Expected = true }
        };

        foreach (var testCase in testCases) {
            // Arrange
            var form = new MockTnTForm { ReadOnly = testCase.Form };
            var instance = RenderTestInputWithForm(form, readOnly: testCase.Local);

            // Assert
            instance.FieldReadonly.Should().Be(testCase.Expected,
                $"Form.ReadOnly={testCase.Form}, Local.ReadOnly={testCase.Local} should result in FieldReadonly={testCase.Expected}");
        }
    }

    [Fact]
    public void OverrideForm_DefaultsToFalse() {
        // Arrange & Act
        var cut = RenderTestInput();

        // Assert
        cut.Instance.OverrideForm.Should().BeFalse();
    }

    [Fact]
    public void WithOverrideFormTrue_FieldDisabled_IgnoresFormDisabled() {
        // Arrange — form is disabled, but the component opts out
        var form = new MockTnTForm { Disabled = true };

        // Act
        var instance = RenderTestInputWithForm(form, disabled: false, overrideForm: true);

        // Assert
        instance.FieldDisabled.Should().BeFalse();
    }

    [Fact]
    public void WithOverrideFormTrue_FieldDisabled_RespectsLocalDisabled() {
        // Arrange — form is NOT disabled, but the component itself is
        var form = new MockTnTForm { Disabled = false };

        // Act
        var instance = RenderTestInputWithForm(form, disabled: true, overrideForm: true);

        // Assert
        instance.FieldDisabled.Should().BeTrue();
    }

    [Fact]
    public void WithOverrideFormTrue_FieldReadonly_IgnoresFormReadOnly() {
        // Arrange — form is read-only, but the component opts out
        var form = new MockTnTForm { ReadOnly = true };

        // Act
        var instance = RenderTestInputWithForm(form, readOnly: false, overrideForm: true);

        // Assert
        instance.FieldReadonly.Should().BeFalse();
    }

    [Fact]
    public void WithOverrideFormTrue_FieldReadonly_RespectsLocalReadOnly() {
        // Arrange — form is NOT read-only, but the component itself is
        var form = new MockTnTForm { ReadOnly = false };

        // Act
        var instance = RenderTestInputWithForm(form, readOnly: true, overrideForm: true);

        // Assert
        instance.FieldReadonly.Should().BeTrue();
    }

    [Fact]
    public void WithOverrideFormTrue_UsesLocalAppearance_NotFormAppearance() {
        // Arrange — form is FilledXS, component wants Outlined
        var form = new MockTnTForm { Appearance = FormAppearance.FilledXS };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.AddAttribute(4, nameof(TestTnTInputBase.Appearance), FormAppearance.Outlined);
                builder.AddAttribute(5, nameof(TestTnTInputBase.OverrideForm), true);
                builder.CloseComponent();
            }));
        });
        var cls = cut.Find("label").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("tnt-form-outlined");
        cls.Should().NotContain("tnt-form-filled");
        cls.Should().NotContain("tnt-form-xs");
    }

    [Fact]
    public void WithOverrideFormTrue_InputNotDisabled_WhenOnlyFormIsDisabled() {
        // Arrange
        var form = new MockTnTForm { Disabled = true };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.AddAttribute(4, nameof(TestTnTInputBase.OverrideForm), true);
                builder.CloseComponent();
            }));
        });

        // Assert
        cut.Find("input").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void WithOverrideFormFalse_FormAppearanceStillTakesPrecedence() {
        // Arrange — verify the default (false) behaviour is unchanged
        var form = new MockTnTForm { Appearance = FormAppearance.Outlined };

        // Act
        var instance = RenderTestInputWithForm(form, appearance: FormAppearance.Filled, overrideForm: false);

        // Assert — form wins
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                var model2 = CreateTestModel();
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model2.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model2.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model2.TestValue = v));
                builder.AddAttribute(4, nameof(TestTnTInputBase.Appearance), FormAppearance.Filled);
                builder.AddAttribute(5, nameof(TestTnTInputBase.OverrideForm), false);
                builder.CloseComponent();
            }));
        });
        var cls = cut.Find("label").GetAttribute("class")!;
        cls.Should().Contain("tnt-form-outlined");
        cls.Should().NotContain("tnt-form-filled");
    }

    [Fact]
    public void WithOverrideFormFalse_FormDisabledStillDisablesInput() {
        // Arrange — verify the default (false) behaviour is unchanged
        var form = new MockTnTForm { Disabled = true };
        var model = CreateTestModel();

        // Act
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
                builder.AddAttribute(4, nameof(TestTnTInputBase.OverrideForm), false);
                builder.CloseComponent();
            }));
        });

        // Assert
        cut.Find("input").HasAttribute("disabled").Should().BeTrue();
    }

    private TestModel CreateTestModel() => new();

    private TestModelWithValidation CreateValidationTestModel() => new();

    private IRenderedComponent<TestTnTInputBase> RenderTestInput(TestModel? model = null, Action<ComponentParameterCollectionBuilder<TestTnTInputBase>>? configure = null) {
        model ??= CreateTestModel();
        return Render<TestTnTInputBase>(parameters => {
            parameters
                .Add(p => p.ValueExpression, () => model.TestValue)
                .Add(p => p.Value, model.TestValue)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));
            configure?.Invoke(parameters);
        });
    }

    private TestTnTInputBase RenderTestInputWithForm(MockTnTForm form, TestModel? model = null, bool? disabled = null, bool? readOnly = null, bool? overrideForm = null, FormAppearance? appearance = null) {
        model ??= CreateTestModel();
        var cut = Render<CascadingValue<ITnTForm>>(parameters => {
            parameters.Add(p => p.Value, form);
            parameters.Add(p => p.IsFixed, true);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => {
                builder.OpenComponent<TestTnTInputBase>(0);
                builder.AddAttribute(1, nameof(TestTnTInputBase.ValueExpression), (Expression<Func<string?>>)(() => model.TestValue));
                builder.AddAttribute(2, nameof(TestTnTInputBase.Value), model.TestValue);
                builder.AddAttribute(3, nameof(TestTnTInputBase.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TestValue = v));

                if (disabled.HasValue)
                    builder.AddAttribute(4, nameof(TestTnTInputBase.Disabled), disabled.Value);
                if (readOnly.HasValue)
                    builder.AddAttribute(5, nameof(TestTnTInputBase.ReadOnly), readOnly.Value);
                if (overrideForm.HasValue)
                    builder.AddAttribute(6, nameof(TestTnTInputBase.OverrideForm), overrideForm.Value);
                if (appearance.HasValue)
                    builder.AddAttribute(7, nameof(TestTnTInputBase.Appearance), appearance.Value);

                builder.CloseComponent();
            }));
        });

        return cut.FindComponent<TestTnTInputBase>().Instance;
    }

    private class MockTnTForm : ITnTForm {
        public FormAppearance Appearance { get; set; } = FormAppearance.Filled;
        public bool Disabled { get; set; }
        public bool ReadOnly { get; set; }
    }

    private class TestModel {
        public string? TestValue { get; set; }
    }

    private class TestModelWithValidation {

        [Required(ErrorMessage = "Test value is required")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Length must be between 2-10 characters")]
        [MaxLength(10)]
        public string? TestValue { get; set; }
    }

    /// <summary>
    ///     Concrete implementation of TnTInputBase for testing purposes
    /// </summary>
    private class TestTnTInputBase : TnTInputBase<string?> {
        public override InputType Type => InputType.Text;

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }
}
