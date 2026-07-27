using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form.TnTInputDateTime;

public class Rendering_Tests : BunitContext {

    public Rendering_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void DateTimeValue_RendersInvariantDateTimeLocalContract() {
        var model = new ValueModel<DateTime> { Value = new(2026, 7, 27, 14, 30, 15) };

        var cut = RenderInput(model);

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("datetime-local");
        input.GetAttribute("value").Should().Be("2026-07-27T14:30:15");
        input.GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
        cut.Instance.Format.Should().Be("yyyy-MM-ddTHH:mm:ss");
    }

    [Fact]
    public void DateTimeOffsetValue_RendersInvariantDateTimeLocalContract() {
        var model = new ValueModel<DateTimeOffset> { Value = new(2026, 7, 27, 14, 30, 15, TimeSpan.FromHours(-6)) };

        var cut = RenderInput(model);

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("datetime-local");
        input.GetAttribute("value").Should().Be("2026-07-27T14:30:15");
        input.GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
    }

    [Fact]
    public void DateOnlyValue_RendersDateContract() {
        var model = new ValueModel<DateOnly> { Value = new(2026, 7, 27) };

        var cut = RenderInput(model);

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("date");
        input.GetAttribute("value").Should().Be("2026-07-27");
        input.GetAttribute("format").Should().Be("yyyy-MM-dd");
    }

    [Fact]
    public void DateOnlyMonthValue_RendersMonthContractAndPreservesAdditionalAttributes() {
        var model = new ValueModel<DateOnly> { Value = new(2026, 7, 1) };

        var cut = RenderInput(model, parameters => parameters
            .Add(component => component.MonthOnly, true)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["data-test"] = "kept" }));

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("month");
        input.GetAttribute("value").Should().Be("2026-07");
        input.GetAttribute("format").Should().Be("yyyy-MM");
        input.GetAttribute("data-test").Should().Be("kept");
        cut.Instance.Format.Should().Be("yyyy-MM");
    }

    [Fact]
    public void TimeOnlyValue_RendersTimeContract() {
        var model = new ValueModel<TimeOnly> { Value = new(14, 30, 15) };

        var cut = RenderInput(model);

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("time");
        input.GetAttribute("value").Should().Be("14:30:15");
        input.GetAttribute("format").Should().Be("HH:mm:ss");
    }

    [Fact]
    public void NullableValue_WhenNull_RendersEmptyValue() {
        var model = new ValueModel<DateTime?>();

        var cut = RenderInput(model);

        cut.Find("input").GetAttribute("value").Should().BeEmpty();
    }

    [Fact]
    public void UnsupportedValueType_ThrowsExactInitializationError() {
        var model = new ValueModel<string> { Value = "not-a-date" };

        var act = () => RenderInput(model);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The type 'System.String' is not a supported DateTime type.");
    }

    private IRenderedComponent<global::NTComponents.TnTInputDateTime<TValue>> RenderInput<TValue>(ValueModel<TValue> model, Action<ComponentParameterCollectionBuilder<global::NTComponents.TnTInputDateTime<TValue>>>? configure = null) {
        return Render<global::NTComponents.TnTInputDateTime<TValue>>(parameters => {
            parameters
                .Add(component => component.ValueExpression, () => model.Value)
                .Add(component => component.Value, model.Value)
                .Add(component => component.ValueChanged, EventCallback.Factory.Create<TValue>(this, value => model.Value = value));
            configure?.Invoke(parameters);
        });
    }

    private sealed class ValueModel<TValue> {
        public TValue Value { get; set; } = default!;
    }
}
