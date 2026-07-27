using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form.TnTInputRadioGroup;

public class Parsing_Tests : BunitContext {

    public Parsing_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void BooleanValue_WhenDifferentRadioSelected_UpdatesModelAndBindAfter() {
        var model = new ValueModel<bool> { Value = true };
        bool? boundValue = null;
        var cut = RenderRadioGroup(model, BooleanRadios(), parameters => parameters
            .Add(component => component.BindAfter, EventCallback.Factory.Create<bool>(this, value => boundValue = value)));

        cut.FindAll("input[type=radio]")[1].Change("False");

        model.Value.Should().BeFalse();
        boundValue.Should().BeFalse();
    }

    [Fact]
    public void NullableBooleanValue_WhenEmptyRadioSelected_SetsModelToNull() {
        var model = new ValueModel<bool?> { Value = true };
        var cut = RenderRadioGroup(model, NullableBooleanRadios());

        cut.FindAll("input[type=radio]")[2].Change(string.Empty);

        model.Value.Should().BeNull();
    }

    [Fact]
    public void NullableBooleanValue_WithValidText_UpdatesModel() {
        var model = new ValueModel<bool?>();
        var cut = RenderRadioGroup(model, NullableBooleanRadios());

        cut.FindAll("input[type=radio]")[1].Change("False");

        model.Value.Should().BeFalse();
    }

    [Fact]
    public void IntegerValue_WithValidText_UpdatesModel() {
        var model = new ValueModel<int>();
        var cut = RenderRadioGroup(model, IntegerRadios());

        cut.Find("input[type=radio]").Change("10");

        model.Value.Should().Be(10);
    }

    [Fact]
    public void BooleanValue_WithMalformedText_KeepsModelAndShowsDisplayNameValidationError() {
        var model = new ValueModel<bool> { Value = true };
        var editContext = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, builder => {
                builder.OpenComponent<global::NTComponents.TnTInputRadioGroup<bool>>(0);
                builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadioGroup<bool>.ValueExpression), (Expression<Func<bool>>)(() => model.Value));
                builder.AddAttribute(2, nameof(global::NTComponents.TnTInputRadioGroup<bool>.Value), model.Value);
                builder.AddAttribute(3, nameof(global::NTComponents.TnTInputRadioGroup<bool>.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(global::NTComponents.TnTInputRadioGroup<bool>.DisplayName), "Consent");
                builder.AddAttribute(5, nameof(global::NTComponents.TnTInputRadioGroup<bool>.ChildContent), BooleanRadios());
                builder.CloseComponent();
            }));

        cut.Find("input[type=radio]").Change("not-a-boolean");

        model.Value.Should().BeTrue();
        cut.Find(".tnt-validation-message").TextContent.Should().Be("The Consent field is not valid.");
    }

    [Fact]
    public void IntegerValue_WithMalformedText_KeepsModelAndShowsFieldValidationError() {
        var model = new ValueModel<int> { Value = 10 };
        var editContext = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, builder => {
                builder.OpenComponent<global::NTComponents.TnTInputRadioGroup<int>>(0);
                builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadioGroup<int>.ValueExpression), (Expression<Func<int>>)(() => model.Value));
                builder.AddAttribute(2, nameof(global::NTComponents.TnTInputRadioGroup<int>.Value), model.Value);
                builder.AddAttribute(3, nameof(global::NTComponents.TnTInputRadioGroup<int>.ValueChanged), EventCallback.Factory.Create<int>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(global::NTComponents.TnTInputRadioGroup<int>.ChildContent), IntegerRadios());
                builder.CloseComponent();
            }));

        cut.Find("input[type=radio]").Change("not-an-integer");

        model.Value.Should().Be(10);
        cut.Find(".tnt-validation-message").TextContent.Should().Be("The Value field is not valid.");
    }

    [Fact]
    public void UnsupportedValueType_OnChangeThrowsTypeSpecificError() {
        var model = new ValueModel<Dictionary<string, string>> { Value = [] };
        var cut = RenderRadioGroup(model, UnsupportedRadios());

        var act = () => cut.Find("input[type=radio]").Change("one");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TnTInputRadioGroup*does not support the type 'System.Collections.Generic.Dictionary`2[System.String,System.String]'.*");
    }

    private static RenderFragment BooleanRadios() => builder => {
        builder.OpenComponent<global::NTComponents.TnTInputRadio<bool>>(0);
        builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadio<bool>.Value), true);
        builder.AddAttribute(2, nameof(global::NTComponents.TnTInputRadio<bool>.Label), "Yes");
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.TnTInputRadio<bool>>(3);
        builder.AddAttribute(4, nameof(global::NTComponents.TnTInputRadio<bool>.Value), false);
        builder.AddAttribute(5, nameof(global::NTComponents.TnTInputRadio<bool>.Label), "No");
        builder.CloseComponent();
    };

    private static RenderFragment NullableBooleanRadios() => builder => {
        builder.OpenComponent<global::NTComponents.TnTInputRadio<bool?>>(0);
        builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadio<bool?>.Value), true);
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.TnTInputRadio<bool?>>(2);
        builder.AddAttribute(3, nameof(global::NTComponents.TnTInputRadio<bool?>.Value), false);
        builder.CloseComponent();
        builder.OpenComponent<global::NTComponents.TnTInputRadio<bool?>>(4);
        builder.AddAttribute(5, nameof(global::NTComponents.TnTInputRadio<bool?>.Value), (bool?)null);
        builder.CloseComponent();
    };

    private static RenderFragment IntegerRadios() => builder => {
        builder.OpenComponent<global::NTComponents.TnTInputRadio<int>>(0);
        builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadio<int>.Value), 10);
        builder.CloseComponent();
    };

    private static RenderFragment UnsupportedRadios() => builder => {
        builder.OpenComponent<global::NTComponents.TnTInputRadio<Dictionary<string, string>>>(0);
        builder.AddAttribute(1, nameof(global::NTComponents.TnTInputRadio<Dictionary<string, string>>.Value), new Dictionary<string, string>());
        builder.CloseComponent();
    };

    private IRenderedComponent<global::NTComponents.TnTInputRadioGroup<TValue>> RenderRadioGroup<TValue>(ValueModel<TValue> model, RenderFragment radios, Action<ComponentParameterCollectionBuilder<global::NTComponents.TnTInputRadioGroup<TValue>>>? configure = null) {
        return Render<global::NTComponents.TnTInputRadioGroup<TValue>>(parameters => {
            parameters
                .Add(component => component.ValueExpression, () => model.Value)
                .Add(component => component.Value, model.Value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<TValue>(this, value => model.Value = value))
                .Add(component => component.ChildContent, radios);
            configure?.Invoke(parameters);
        });
    }

    private sealed class ValueModel<TValue> {
        public TValue Value { get; set; } = default!;
    }
}
