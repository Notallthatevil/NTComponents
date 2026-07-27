using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTFormControlBase_Tests : BunitContext {

    public NTFormControlBase_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void FormState_IsInheritedByDefault() {
        var model = new ControlModel();

        var cut = RenderInForm(model, form => form
            .Add(component => component.Density, NTFormDensity.Dense)
            .Add(component => component.Disabled, true)
            .Add(component => component.ReadOnly, true));

        var root = cut.Find(".test-control");
        root.GetAttribute("data-density").Should().Be(nameof(NTFormDensity.Dense));
        root.GetAttribute("data-disabled").Should().Be("true");
        root.GetAttribute("data-readonly").Should().Be("true");
    }

    [Fact]
    public void ExplicitControlState_OverridesFormState() {
        var model = new ControlModel();

        var cut = RenderInForm(model,
            form => form
                .Add(component => component.Density, NTFormDensity.Dense)
                .Add(component => component.Disabled, true)
                .Add(component => component.ReadOnly, true),
            control => control
                .Add(component => component.Density, NTFormDensity.Comfortable)
                .Add(component => component.Disabled, false)
                .Add(component => component.ReadOnly, false));

        var root = cut.Find(".test-control");
        root.GetAttribute("data-density").Should().Be(nameof(NTFormDensity.Comfortable));
        root.GetAttribute("data-disabled").Should().Be("false");
        root.GetAttribute("data-readonly").Should().Be("false");
    }

    [Fact]
    public void ExplicitErrorAndExternalDescription_RenderMergedAccessibilityContract() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> { ["ARIA-DESCRIBEDBY"] = "external-help" };

        var cut = RenderControl(model, control => control
            .Add(component => component.ErrorText, "Explicit error")
            .Add(component => component.AdditionalAttributes, attributes));

        var input = cut.Find("input");
        cut.Find(".test-control").GetAttribute("data-error").Should().Be("Explicit error");
        input.GetAttribute("aria-describedby").Should().Be($"external-help {input.Id}-error");
    }

    [Fact]
    public void SupportingTextWithoutExternalDescription_RendersInternalDescription() {
        var model = new ControlModel();

        var cut = RenderControl(model, control => control.Add(component => component.SupportingText, "Helpful text"));

        var input = cut.Find("input");
        cut.Find(".test-control").GetAttribute("data-supporting").Should().Be("Helpful text");
        input.GetAttribute("aria-describedby").Should().Be($"{input.Id}-supporting");
    }

    [Fact]
    public void ExternalDescriptionWithoutInternalText_IsPreserved() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> { ["aria-describedby"] = "external-help" };

        var cut = RenderControl(model, control => control.Add(component => component.AdditionalAttributes, attributes));

        cut.Find("input").GetAttribute("aria-describedby").Should().Be("external-help");
    }

    [Fact]
    public void NoDescription_OmitsAriaDescribedBy() {
        var model = new ControlModel();

        var cut = RenderControl(model);

        cut.Find("input").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "extra-description")]
    [InlineData("Supporting text", "supporting")]
    public void DerivedControl_CanAppendItsOwnDescriptionId(string? supportingText, string expectedDescription) {
        var model = new ControlModel();

        var cut = RenderControl(model, control => control
            .Add(component => component.SupportingText, supportingText)
            .Add(component => component.AppendExtraDescription, true));

        var describedBy = cut.Find("input").GetAttribute("aria-describedby");
        if (supportingText is null) {
            describedBy.Should().Be(expectedDescription);
        }
        else {
            describedBy.Should().Be($"{cut.Find("input").Id}-{expectedDescription} extra-description");
        }
    }

    [Fact]
    public void RequiredControl_InheritsFormSupportingText() {
        var model = new ControlModel();

        var cut = RenderInForm(model,
            form => form
                .Add(component => component.ShowRequiredSupportingText, true)
                .Add(component => component.RequiredSupportingText, "Required by form"),
            control => control.Add(component => component.Required, true));

        cut.Find(".test-control").GetAttribute("data-supporting").Should().Be("Required by form");
    }

    [Fact]
    public void ExplicitSupportingText_OverridesRequiredFormText() {
        var model = new ControlModel();

        var cut = RenderInForm(model,
            form => form
                .Add(component => component.ShowRequiredSupportingText, true)
                .Add(component => component.RequiredSupportingText, "Required by form"),
            control => control
                .Add(component => component.Required, true)
                .Add(component => component.SupportingText, "Control guidance"));

        cut.Find(".test-control").GetAttribute("data-supporting").Should().Be("Control guidance");
    }

    [Fact]
    public void ValidationMessage_RendersAsCurrentErrorAndFieldClass() {
        var model = new ControlModel();
        var editContext = new EditContext(model);
        var field = editContext.Field(nameof(ControlModel.Value));
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Validation failed");

        var cut = RenderInForm(model, editContext: editContext);

        var root = cut.Find(".test-control");
        root.GetAttribute("data-error").Should().Be("Validation failed");
        root.ClassList.Should().Contain("nt-invalid");
        cut.Find("input").GetAttribute("aria-describedby").Should().EndWith("-error");
    }

    [Fact]
    public void DisabledValidationMessage_HidesEditContextError() {
        var model = new ControlModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(ControlModel.Value)), "Validation failed");

        var cut = RenderInForm(model, control: control => control.Add(component => component.DisableValidationMessage, true), editContext: editContext);

        cut.Find(".test-control").HasAttribute("data-error").Should().BeFalse();
        cut.Find("input").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Blur_MarksUnmodifiedFieldAndInvokesCallbackForEveryEvent() {
        var model = new ControlModel();
        var editContext = new EditContext(model);
        var callbackCount = 0;
        var cut = RenderInForm(model,
            control: control => control.Add(component => component.OnBlurCallback, EventCallback.Factory.Create<FocusEventArgs>(this, _ => callbackCount++)),
            editContext: editContext);
        var field = editContext.Field(nameof(ControlModel.Value));

        cut.Find("input").Blur();
        cut.Find("input").Blur();

        editContext.IsModified(field).Should().BeTrue();
        callbackCount.Should().Be(2);
        cut.Find(".test-control").ClassList.Should().Contain("nt-modified");
    }

    [Fact]
    public void FilteredAttributes_PreserveCallerDataAndDerivedOverridesWithoutReplacingOwnedSemantics() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> {
            ["class"] = "caller-class",
            ["id"] = "caller-id",
            ["name"] = "caller-name",
            ["disabled"] = false,
            ["readonly"] = false,
            ["data-extra"] = "caller-extra",
            ["data-shared"] = "caller-value"
        };
        var derivedAttributes = new Dictionary<string, object?> {
            ["DATA-SHARED"] = "derived-value",
            ["data-derived"] = "derived-extra"
        };

        var cut = RenderControl(model, control => control
            .Add(component => component.ElementId, "owned-id")
            .Add(component => component.Disabled, true)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.AdditionalAttributes, attributes)
            .Add(component => component.DerivedAttributes, derivedAttributes));

        var input = cut.Find("input");
        input.Id.Should().Be("owned-id");
        input.GetAttribute("name").Should().Be("caller-name");
        input.HasAttribute("disabled").Should().BeTrue();
        input.HasAttribute("readonly").Should().BeTrue();
        input.ClassList.Should().NotContain("caller-class");
        input.GetAttribute("data-extra").Should().Be("caller-extra");
        input.GetAttribute("data-shared").Should().Be("derived-value");
        input.GetAttribute("data-derived").Should().Be("derived-extra");
    }

    [Fact]
    public void OnlyOwnedAdditionalAttributes_AreRemovedWithoutLeavingCallerClass() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> {
            ["class"] = "caller-class",
            ["id"] = "caller-id",
            ["name"] = "caller-name"
        };

        var cut = RenderControl(model, control => control
            .Add(component => component.ElementId, "owned-id")
            .Add(component => component.AdditionalAttributes, attributes));

        var input = cut.Find("input");
        input.Id.Should().Be("owned-id");
        input.GetAttribute("name").Should().Be("caller-name");
        input.HasAttribute("class").Should().BeFalse();
    }

    [Fact]
    public void BlankAdditionalNameAndDescription_FallBackToBoundNameAndOmitDescription() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> {
            ["name"] = " ",
            ["aria-describedby"] = " "
        };

        var cut = RenderControl(model, control => control.Add(component => component.AdditionalAttributes, attributes));

        cut.Find("input").GetAttribute("name").Should().Be(nameof(ControlModel.Value));
        cut.Find("input").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void NullAdditionalNameAndDescription_FallBackToBoundNameAndOmitDescription() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> {
            ["name"] = null!,
            ["aria-describedby"] = null!
        };

        var cut = RenderControl(model, control => control.Add(component => component.AdditionalAttributes, attributes));

        cut.Find("input").GetAttribute("name").Should().Be(nameof(ControlModel.Value));
        cut.Find("input").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void GeneratedInputId_IsStableForTheSameBoundField() {
        var model = new ControlModel();

        var first = RenderControl(model);
        var second = RenderControl(model);

        first.Find("input").Id.Should().StartWith("test-control-");
        second.Find("input").Id.Should().Be(first.Find("input").Id);
        first.Find("input").GetAttribute("name").Should().Be("model.Value");
    }

    [Fact]
    public void SubmitValueFalse_OmitsNameButKeepsStableInputId() {
        var model = new ControlModel();

        var cut = RenderControl(model, control => control.Add(component => component.SubmitValue, false));

        var input = cut.Find("input");
        input.HasAttribute("name").Should().BeFalse();
        input.Id.Should().StartWith("test-control-");
    }

    [Fact]
    public void ExplicitElementId_ControlsInputAndLabelAssociation() {
        var model = new ControlModel();

        var cut = RenderControl(model, control => control
            .Add(component => component.ElementId, "explicit-id")
            .Add(component => component.Label, "Visible label"));

        cut.Find("input").Id.Should().Be("explicit-id");
        cut.Find("label").GetAttribute("for").Should().Be("explicit-id");
        cut.Find(".test-control").GetAttribute("data-has-label").Should().Be("true");
    }

    [Fact]
    public void CommonElementParameters_RenderOnNativeInput() {
        var model = new ControlModel();

        var cut = RenderControl(model, control => control
            .Add(component => component.AutoFocus, true)
            .Add(component => component.ElementLang, "en-CA")
            .Add(component => component.ElementTitle, "Field title"));

        var input = cut.Find("input");
        input.HasAttribute("autofocus").Should().BeTrue();
        input.GetAttribute("lang").Should().Be("en-CA");
        input.GetAttribute("title").Should().Be("Field title");
    }

    [Fact]
    public async Task SetFocusAsync_FocusesRenderedInput() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = RenderControl(new ControlModel());

        await cut.Instance.SetFocusAsync();

        JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void DefaultBaseContract_UsesDefaultPrefixAndDoesNotClaimRequiredSupportingText() {
        var model = new ControlModel();

        var cut = RenderInForm(model,
            form => form
                .Add(component => component.ShowRequiredSupportingText, true)
                .Add(component => component.RequiredSupportingText, "Required by form"),
            control => control
                .Add(component => component.Required, true)
                .Add(component => component.UseDefaultBaseContract, true));

        cut.Find("input").Id.Should().StartWith("nt-form-control-");
        cut.Find(".test-control").HasAttribute("data-supporting").Should().BeFalse();
    }

    [Fact]
    public void PunctuationOnlyExplicitName_GeneratesSanitizedStableId() {
        var model = new ControlModel();
        var attributes = new Dictionary<string, object> { ["name"] = "---" };

        var cut = RenderControl(model, control => control.Add(component => component.AdditionalAttributes, attributes));

        cut.Find("input").GetAttribute("name").Should().Be("---");
        cut.Find("input").Id.Should().StartWith("test-control-field-");
    }

    [Fact]
    public void NestedArrayAndDictionaryBindings_RenderDeterministicNames() {
        var model = new ControlModel();
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var nested = RenderControl(model, () => model.Nested.Value, model.Nested.Value, value => model.Nested.Value = value,
            control => control.Add(component => component.AdditionalAttributes, blankExplicitName));
        var array = RenderControl(model, () => model.Items[1].Value, model.Items[1].Value, value => model.Items[1].Value = value,
            control => control.Add(component => component.AdditionalAttributes, blankExplicitName));
        var dictionary = RenderControl(model, () => model.KeyedItems["primary"].Value, model.KeyedItems["primary"].Value, value => model.KeyedItems["primary"].Value = value,
            control => control.Add(component => component.AdditionalAttributes, blankExplicitName));

        nested.Find("input").GetAttribute("name").Should().Be("Nested.Value");
        array.Find("input").GetAttribute("name").Should().Be("Items[1].Value");
        dictionary.Find("input").GetAttribute("name").Should().Be("KeyedItems[primary].Value");
    }

    [Fact]
    public void IndexedNameFallback_HandlesConvertedIndexExpression() {
        var model = new ControlModel();
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var convertedIndex = RenderControl(model, () => model.ObjectKeyedItems[(object)1].Value, model.ObjectKeyedItems[1].Value, value => model.ObjectKeyedItems[1].Value = value,
            control => control.Add(component => component.AdditionalAttributes, blankExplicitName));

        convertedIndex.Find("input").GetAttribute("name").Should().Be("ObjectKeyedItems[1].Value");
    }

    [Fact]
    public void CapturedLocalBinding_FallsBackToFieldIdentifierName() {
        var model = new ControlModel();
        string? localValue = null;
        var blankExplicitName = new Dictionary<string, object> { ["name"] = " " };

        var cut = RenderControl(model, () => localValue, localValue, value => localValue = value,
            control => control.Add(component => component.AdditionalAttributes, blankExplicitName));

        cut.Find("input").GetAttribute("name").Should().Be(nameof(localValue));
    }

    private IRenderedComponent<TestFormControl> RenderControl(ControlModel model, Action<ComponentParameterCollectionBuilder<TestFormControl>>? configure = null) {
        return RenderControl(model, () => model.Value, model.Value, value => model.Value = value, configure);
    }

    private IRenderedComponent<TestFormControl> RenderControl(ControlModel model, Expression<Func<string?>> expression, string? value, Action<string?> valueChanged, Action<ComponentParameterCollectionBuilder<TestFormControl>>? configure = null) {
        return Render<TestFormControl>(parameters => {
            parameters
                .Add(component => component.ValueExpression, expression)
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create(this, valueChanged));
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<TestFormControl> RenderInForm(ControlModel model, Action<ComponentParameterCollectionBuilder<NTForm>>? form = null,
        Action<ComponentParameterCollectionBuilder<TestFormControl>>? control = null, EditContext? editContext = null) {
        editContext ??= new EditContext(model);
        var renderedForm = Render<NTForm>(parameters => {
            parameters.Add(component => component.EditContext, editContext);
            form?.Invoke(parameters);
        });
        var cascade = Render<CascadingValue<NTForm>>(parameters => parameters
            .Add(component => component.Value, renderedForm.Instance)
            .Add(component => component.IsFixed, true)
            .AddChildContent<CascadingValue<EditContext>>(editContextParameters => editContextParameters
                .Add(component => component.Value, editContext)
                .Add(component => component.IsFixed, true)
                .AddChildContent<TestFormControl>(child => {
                    child
                        .Add(component => component.ValueExpression, () => model.Value)
                        .Add(component => component.Value, model.Value)
                        .Add(component => component.ValueChanged, EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                    control?.Invoke(child);
                })));
        return cascade.FindComponent<TestFormControl>();
    }

    private sealed class ControlModel {
        public string? Value { get; set; }
        public ControlItem Nested { get; } = new();
        public ControlItem[] Items { get; } = [new(), new()];
        public Dictionary<string, ControlItem> KeyedItems { get; } = new() { ["primary"] = new() };
        public Dictionary<object, ControlItem> ObjectKeyedItems { get; } = new() { [1] = new() };

    }

    private sealed class ControlItem {
        public string? Value { get; set; }
    }

    private sealed class TestFormControl : NTFormControlBase<string?> {
        [Parameter]
        public bool AppendExtraDescription { get; set; }

        [Parameter]
        public IReadOnlyDictionary<string, object?>? DerivedAttributes { get; set; }

        [Parameter]
        public bool Required { get; set; }

        [Parameter]
        public bool UseDefaultBaseContract { get; set; }

        protected override bool HasRequiredSupportingText => UseDefaultBaseContract ? base.HasRequiredSupportingText : Required;
        protected override string InputIdPrefix => UseDefaultBaseContract ? base.InputIdPrefix : "test-control";

        protected override string? BuildDescribedBy(bool hasErrorText, bool hasSupportingText) {
            var describedBy = base.BuildDescribedBy(hasErrorText, hasSupportingText);
            return AppendExtraDescription ? AppendDescribedById(describedBy, "extra-description") : describedBy;
        }

        protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage) {
            result = value;
            validationErrorMessage = string.Empty;
            return true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            var errorText = CurrentErrorText;
            var supportingText = CurrentSupportingText;

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", AppendFieldCssClass("test-control"));
            builder.AddAttribute(2, "data-density", EffectiveDensity.ToString());
            builder.AddAttribute(3, "data-disabled", FieldDisabled.ToString().ToLowerInvariant());
            builder.AddAttribute(4, "data-readonly", FieldReadOnly.ToString().ToLowerInvariant());
            builder.AddAttribute(5, "data-has-label", HasLabel.ToString().ToLowerInvariant());
            builder.AddAttribute(6, "data-error", errorText);
            builder.AddAttribute(7, "data-supporting", supportingText);

            builder.OpenElement(10, "label");
            builder.AddAttribute(11, "id", LabelId);
            builder.AddAttribute(12, "for", EffectiveInputId);
            builder.AddContent(13, Label);
            builder.CloseElement();

            builder.OpenElement(20, "input");
            var filteredAttributes = BuildFilteredAttributes(["id", "name", "disabled", "readonly", "aria-describedby"], DerivedAttributes);
            if (filteredAttributes is not null) {
                builder.AddMultipleAttributes(21, filteredAttributes.Select(attribute => new KeyValuePair<string, object>(attribute.Key, attribute.Value!)));
            }
            builder.AddAttribute(22, "id", EffectiveInputId);
            builder.AddAttribute(23, "name", ElementName);
            builder.AddAttribute(24, "disabled", FieldDisabled);
            builder.AddAttribute(25, "readonly", FieldReadOnly);
            builder.AddAttribute(26, "aria-describedby", BuildDescribedBy(errorText is not null, supportingText is not null));
            builder.AddAttribute(27, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, OnBlurAsync));
            builder.AddAttribute(28, "autofocus", AutoFocus);
            builder.AddAttribute(29, "lang", ElementLang);
            builder.AddAttribute(30, "title", ElementTitle);
            builder.AddElementReferenceCapture(31, element => Element = element);
            builder.CloseElement();

            if (errorText is not null) {
                builder.OpenElement(40, "span");
                builder.AddAttribute(41, "id", ErrorTextId);
                builder.AddContent(42, errorText);
                builder.CloseElement();
            }
            else if (supportingText is not null) {
                builder.OpenElement(50, "span");
                builder.AddAttribute(51, "id", SupportingTextId);
                builder.AddContent(52, supportingText);
                builder.CloseElement();
            }

            builder.CloseElement();
        }
    }
}
