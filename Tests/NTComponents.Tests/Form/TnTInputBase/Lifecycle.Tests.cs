using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Form.TnTInputBase;

public class Lifecycle_Tests : BunitContext {

    public Lifecycle_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var tooltipModule = JSInterop.SetupModule("./_content/NTComponents/Tooltip/TnTTooltip.razor.js");
        tooltipModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        tooltipModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void ChangeBinding_DefersBindAfterUntilBlurAndMarksFieldModified() {
        var model = new InputModel();
        var editContext = new EditContext(model);
        var bindAfterValues = new List<string?>();
        var blurCount = 0;
        var cut = RenderInEditForm(model, editContext, input => input
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValues.Add(value)))
            .Add(component => component.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurCount++)));

        cut.Find("input").Change("updated");

        model.Value.Should().Be("updated");
        bindAfterValues.Should().BeEmpty();

        cut.Find("input").Blur();

        bindAfterValues.Should().Equal("updated");
        blurCount.Should().Be(1);
        editContext.IsModified(editContext.Field(nameof(InputModel.Value))).Should().BeTrue();
    }

    [Fact]
    public void InputBinding_InvokesBindAfterImmediatelyAndDoesNotRepeatOnBlur() {
        var model = new InputModel();
        var editContext = new EditContext(model);
        var bindAfterValues = new List<string?>();
        var cut = RenderInEditForm(model, editContext, input => input
            .Add(component => component.BindOnInput, true)
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValues.Add(value))));

        cut.Find("input").Input("live-value");
        cut.Find("input").Blur();

        model.Value.Should().Be("live-value");
        bindAfterValues.Should().Equal("live-value");
    }

    [Fact]
    public void ExplicitErrorMessage_TakesPrecedenceOverEditContextValidation() {
        var model = new InputModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(InputModel.Value)), "Validation error");

        var cut = RenderInEditForm(model, editContext, input => input.Add(component => component.ErrorMessage, "Explicit error"));

        cut.Find(".tnt-validation-message").TextContent.Should().Be("Explicit error");
        cut.Markup.Should().NotContain("Validation error");
    }

    [Fact]
    public void DisableValidationMessage_HidesEditContextValidationText() {
        var model = new InputModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(InputModel.Value)), "Validation error");

        var cut = RenderInEditForm(model, editContext, input => input.Add(component => component.DisableValidationMessage, true));

        cut.FindAll(".tnt-supporting-text").Should().BeEmpty();
    }

    [Fact]
    public void IconsAndTooltip_AreConfiguredForTheirRenderedPositions() {
        var model = new InputModel();
        var startIcon = new MaterialIcon { Icon = "home" };
        var endIcon = new MaterialIcon { Icon = "search" };
        var tooltipIcon = new MaterialIcon { Icon = "help" };

        var cut = RenderInput(model, input => input
            .Add(component => component.StartIcon, startIcon)
            .Add(component => component.EndIcon, endIcon)
            .Add(component => component.TooltipIcon, tooltipIcon)
            .Add(component => component.Tooltip, builder => builder.AddContent(0, "Tooltip content")));

        cut.Find(".tnt-start-icon").TextContent.Should().Be("home");
        cut.Find(".tnt-end-icon").TextContent.Should().Be("search");
        cut.Find(".tnt-tooltip-icon").ClassList.Should().Contain("mi-small");
        cut.Markup.Should().Contain("Tooltip content");
    }

    [Fact]
    public void ExplicitConstraintAttributes_RenderAsNativeInputContract() {
        var attributes = new Dictionary<string, object> {
            ["maxlength"] = 7,
            ["minlength"] = 2,
            ["min"] = "a",
            ["max"] = "z",
            ["required"] = true
        };

        var cut = RenderInput(new InputModel(), input => input.Add(component => component.AdditionalAttributes, attributes));

        var nativeInput = cut.Find("input:not(.direct-change)");
        nativeInput.GetAttribute("maxlength").Should().Be("7");
        nativeInput.GetAttribute("minlength").Should().Be("2");
        nativeInput.GetAttribute("min").Should().Be("a");
        nativeInput.GetAttribute("max").Should().Be("z");
        nativeInput.HasAttribute("required").Should().BeTrue();
    }

    [Fact]
    public void NullRangeAttributes_OmitNativeMinAndMaxValues() {
        var attributes = new Dictionary<string, object> {
            ["min"] = null!,
            ["max"] = null!,
            ["minlength"] = null!,
            ["maxlength"] = null!
        };

        var cut = RenderInput(new InputModel(), input => input.Add(component => component.AdditionalAttributes, attributes));

        var nativeInput = cut.Find("input:not(.direct-change)");
        nativeInput.HasAttribute("min").Should().BeFalse();
        nativeInput.HasAttribute("max").Should().BeFalse();
        nativeInput.HasAttribute("minlength").Should().BeFalse();
        nativeInput.HasAttribute("maxlength").Should().BeFalse();
    }

    [Fact]
    public void MissingValueExpression_ThrowsExactConfigurationError() {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<LifecycleInput>());

        exception.Message.Should().Contain("requires a value for the 'ValueExpression' parameter");
    }

    [Fact]
    public void BlurWithoutEditContext_InvokesCallbacksWithoutValidationState() {
        var model = new InputModel { Value = "value" };
        var bindAfterValues = new List<string?>();
        var blurCount = 0;
        var cut = RenderInput(model, input => input
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValues.Add(value)))
            .Add(component => component.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => blurCount++)));

        cut.Find("input:not(.direct-change)").Blur();

        bindAfterValues.Should().Equal("value");
        blurCount.Should().Be(1);
    }

    [Fact]
    public void DerivedChangeHandler_UsesTypedValueAndInvokesBindAfter() {
        var model = new InputModel { Value = "initial" };
        var bindAfterValues = new List<string?>();
        var cut = RenderInput(model, input => input.Add(component => component.BindAfter, EventCallback.Factory.Create<string?>(this, value => bindAfterValues.Add(value))));

        cut.Find(".direct-change").Change("updated");

        model.Value.Should().Be("updated");
        bindAfterValues.Should().Equal("updated");
    }

    [Fact]
    public void DerivedNumericChangeHandler_DefaultsStringEventValueAndInvokesBindAfter() {
        var model = new NumericInputModel { Value = 7 };
        var bindAfterValues = new List<int>();
        var cut = Render<LifecycleNumberInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Value = value))
            .Add(component => component.BindAfter, EventCallback.Factory.Create<int>(this, value => bindAfterValues.Add(value))));

        cut.Find(".direct-change").Change("42");

        model.Value.Should().Be(0);
        bindAfterValues.Should().Equal(0);
    }

    [Fact]
    public void BlankExplicitName_UsesExpressionFallbackForIndexedBindings() {
        var model = new InputModel();
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var array = RenderInput(model, () => model.Items[1].Value, model.Items[1].Value, value => model.Items[1].Value = value,
            input => input.Add(component => component.AdditionalAttributes, blankExplicitName));
        var dictionary = RenderInput(model, () => model.KeyedItems["primary"].Value, model.KeyedItems["primary"].Value, value => model.KeyedItems["primary"].Value = value,
            input => input.Add(component => component.AdditionalAttributes, blankExplicitName));

        array.Find("input:not(.direct-change)").GetAttribute("name").Should().Be("Items[1].Value");
        dictionary.Find("input:not(.direct-change)").GetAttribute("name").Should().Be("KeyedItems[primary].Value");
    }

    [Fact]
    public void IndexedNameFallback_HandlesConvertedIndexExpression() {
        var model = new InputModel();
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var convertedIndex = RenderInput(model, () => model.ObjectKeyedItems[(object)1].Value, model.ObjectKeyedItems[1].Value, value => model.ObjectKeyedItems[1].Value = value,
            input => input.Add(component => component.AdditionalAttributes, blankExplicitName));

        convertedIndex.Find("input:not(.direct-change)").GetAttribute("name").Should().Be("ObjectKeyedItems[1].Value");
    }

    [Fact]
    public void CapturedLocalBinding_FallsBackToFieldIdentifierName() {
        var model = new InputModel();
        string? localValue = null;
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var cut = RenderInput(model, () => localValue, localValue, value => localValue = value,
            input => input.Add(component => component.AdditionalAttributes, blankExplicitName));

        cut.Find("input:not(.direct-change)").GetAttribute("name").Should().Be(nameof(localValue));
    }

    [Fact]
    public void ConvertedObjectBinding_RendersWithoutStringOnlyConstraints() {
        var model = new InputModel();

        var cut = Render<LifecycleObjectInput>(parameters => parameters
            .Add(component => component.ValueExpression, () => (object?)model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<object?>(this, _ => { })));

        cut.Find("input").HasAttribute("maxlength").Should().BeFalse();
        cut.Find("input").HasAttribute("minlength").Should().BeFalse();
    }

    [Fact]
    public async Task SetFocusAsync_FocusesRenderedNativeInput() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = RenderInput(new InputModel());

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Theory]
    [InlineData(FormAppearance.FilledCompact, "tnt-form-filled", "tnt-form-compact")]
    [InlineData(FormAppearance.OutlinedCompact, "tnt-form-outlined", "tnt-form-compact")]
    [InlineData(FormAppearance.FilledXS, "tnt-form-filled", "tnt-form-xs")]
    [InlineData(FormAppearance.OutlinedXS, "tnt-form-outlined", "tnt-form-xs")]
    public void CompactAppearance_RendersExpectedPublicClasses(FormAppearance appearance, string baseClass, string sizeClass) {
        var cut = RenderInput(new InputModel(), input => input.Add(component => component.Appearance, appearance));

        cut.Find("label").ClassList.Should().Contain(baseClass);
        cut.Find("label").ClassList.Should().Contain(sizeClass);
    }

    [Fact]
    public void UnsupportedAppearance_ThrowsNotSupportedException() {
        var model = new InputModel();

        Assert.Throws<NotSupportedException>(() => RenderInput(model, input => input.Add(component => component.Appearance, (FormAppearance)int.MaxValue)));
    }

    private IRenderedComponent<LifecycleInput> RenderInput(InputModel model, Action<ComponentParameterCollectionBuilder<LifecycleInput>>? configure = null) {
        return RenderInput(model, () => model.Value, model.Value, value => model.Value = value, configure);
    }

    private IRenderedComponent<LifecycleInput> RenderInput(InputModel model, System.Linq.Expressions.Expression<Func<string?>> expression, string? value, Action<string?> valueChanged,
        Action<ComponentParameterCollectionBuilder<LifecycleInput>>? configure = null) {
        return Render<LifecycleInput>(parameters => {
            parameters
                .Add(component => component.ValueExpression, expression)
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create(this, valueChanged));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<LifecycleInput> RenderInEditForm(InputModel model, EditContext editContext, Action<ComponentParameterCollectionBuilder<LifecycleInput>>? configure = null) {
        var form = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.IsFixed, true)
            .AddChildContent<LifecycleInput>(input => {
                input
                    .Add(component => component.ValueExpression, () => model.Value)
                    .Add(component => component.Value, model.Value)
                    .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                configure?.Invoke(input);
            }));
        return form.FindComponent<LifecycleInput>();
    }

    private sealed class InputModel {
        public string? Value { get; set; }
        public InputItem[] Items { get; } = [new(), new()];
        public Dictionary<string, InputItem> KeyedItems { get; } = new() { ["primary"] = new() };
        public Dictionary<object, InputItem> ObjectKeyedItems { get; } = new() { [1] = new() };

    }

    private sealed class InputItem {
        public string? Value { get; set; }
    }

    private sealed class NumericInputModel {
        public int Value { get; set; }
    }

    private sealed class LifecycleInput : global::NTComponents.TnTInputBase<string?> {
        public override InputType Type => InputType.Text;

        protected override void RenderInputElement(RenderTreeBuilder builder) {
            base.RenderInputElement(builder);
            builder.OpenElement(100, "input");
            builder.AddAttribute(101, "class", "direct-change");
            builder.AddAttribute(102, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, OnChangeAsync));
            builder.CloseElement();
        }

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }

    private sealed class LifecycleNumberInput : global::NTComponents.TnTInputBase<int> {
        public override InputType Type => InputType.Number;

        protected override void RenderInputElement(RenderTreeBuilder builder) {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "class", "direct-change");
            builder.AddAttribute(2, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, OnChangeAsync));
            builder.CloseElement();
        }

        protected override bool TryParseValueFromString(string? value, out int result, out string validationErrorMessage) {
            var parsed = int.TryParse(value, out result);
            validationErrorMessage = parsed ? string.Empty : "Invalid number";
            return parsed;
        }
    }

    private sealed class LifecycleObjectInput : global::NTComponents.TnTInputBase<object?> {
        public override InputType Type => InputType.Text;

        protected override bool TryParseValueFromString(string? value, out object? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }
    }
}
