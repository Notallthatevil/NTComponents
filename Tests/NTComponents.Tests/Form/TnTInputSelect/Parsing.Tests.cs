using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form.TnTInputSelect;

public class Parsing_Tests : BunitContext {

    public Parsing_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void BooleanValue_WithValidText_UpdatesModel() {
        var model = new ValueModel<bool>();
        var cut = RenderSelect(model, BooleanOptions());

        cut.Find("select").Change("true");

        model.Value.Should().BeTrue();
    }

    [Fact]
    public void NullableBooleanValue_WhenCleared_SetsModelToNull() {
        var model = new ValueModel<bool?> { Value = true };
        var cut = RenderSelect(model, BooleanOptions());

        cut.Find("select").Change(string.Empty);

        model.Value.Should().BeNull();
    }

    [Fact]
    public void NullableBooleanValue_WithValidText_UpdatesModel() {
        var model = new ValueModel<bool?>();
        var cut = RenderSelect(model, BooleanOptions());

        cut.Find("select").Change("false");

        model.Value.Should().BeFalse();
    }

    [Fact]
    public void IntegerValue_WithValidText_UpdatesModel() {
        var model = new ValueModel<int>();
        var cut = RenderSelect(model, StringOptions());

        cut.Find("select").Change("3");

        model.Value.Should().Be(3);
    }

    [Fact]
    public void BooleanValue_WithMalformedText_KeepsModelAndShowsDisplayNameValidationError() {
        var model = new ValueModel<bool> { Value = true };
        var editContext = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, builder => {
                builder.OpenComponent<global::NTComponents.TnTInputSelect<bool>>(0);
                builder.AddAttribute(1, nameof(global::NTComponents.TnTInputSelect<bool>.ValueExpression), (Expression<Func<bool>>)(() => model.Value));
                builder.AddAttribute(2, nameof(global::NTComponents.TnTInputSelect<bool>.Value), model.Value);
                builder.AddAttribute(3, nameof(global::NTComponents.TnTInputSelect<bool>.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Value = value));
                builder.AddAttribute(4, nameof(global::NTComponents.TnTInputSelect<bool>.DisplayName), "Consent");
                builder.AddAttribute(5, nameof(global::NTComponents.TnTInputSelect<bool>.ChildContent), BooleanOptions());
                builder.CloseComponent();
            }));

        cut.Find("select").Change("not-a-boolean");

        model.Value.Should().BeTrue();
        cut.Find(".tnt-validation-message").TextContent.Should().Be("The Consent field is not valid.");
    }

    [Fact]
    public void MultipleValue_WithSeveralSelections_UpdatesArrayAndBindAfter() {
        var model = new ValueModel<string[]?> { Value = [] };
        string[]? boundValue = null;
        var cut = RenderSelect(model, StringOptions(), parameters => parameters
            .Add(component => component.BindAfter, EventCallback.Factory.Create<string[]?>(this, value => boundValue = value)));

        cut.Find("select").Change(new ChangeEventArgs { Value = new[] { "one", "three" } });

        model.Value.Should().Equal("one", "three");
        boundValue.Should().Equal("one", "three");
    }

    [Fact]
    public void UnsupportedValueType_OnChangeThrowsTypeSpecificError() {
        var model = new ValueModel<Dictionary<string, string>> { Value = [] };
        var cut = RenderSelect(model, StringOptions());

        var act = () => cut.Find("select").Change("one");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TnTInputSelect*does not support the type 'System.Collections.Generic.Dictionary`2[System.String,System.String]'.*");
    }

    private static RenderFragment BooleanOptions() => builder => {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", "true");
        builder.AddContent(2, "Yes");
        builder.CloseElement();
        builder.OpenElement(3, "option");
        builder.AddAttribute(4, "value", "false");
        builder.AddContent(5, "No");
        builder.CloseElement();
    };

    private static RenderFragment StringOptions() => builder => {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", "one");
        builder.AddContent(2, "One");
        builder.CloseElement();
        builder.OpenElement(3, "option");
        builder.AddAttribute(4, "value", "three");
        builder.AddContent(5, "Three");
        builder.CloseElement();
    };

    private IRenderedComponent<global::NTComponents.TnTInputSelect<TValue>> RenderSelect<TValue>(ValueModel<TValue> model, RenderFragment options, Action<ComponentParameterCollectionBuilder<global::NTComponents.TnTInputSelect<TValue>>>? configure = null) {
        return Render<global::NTComponents.TnTInputSelect<TValue>>(parameters => {
            parameters
                .Add(component => component.ValueExpression, () => model.Value)
                .Add(component => component.Value, model.Value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<TValue>(this, value => model.Value = value))
                .Add(component => component.ChildContent, options);
            configure?.Invoke(parameters);
        });
    }

    private sealed class ValueModel<TValue> {
        public TValue Value { get; set; } = default!;
    }
}
