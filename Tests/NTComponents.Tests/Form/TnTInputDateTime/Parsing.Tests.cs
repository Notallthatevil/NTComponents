using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form.TnTInputDateTime;

public class Parsing_Tests : BunitContext {

    public Parsing_Tests() => SetRendererInfo(new RendererInfo("WebAssembly", true));

    [Fact]
    public void DateTimeInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateTime> { Value = new(2000, 1, 1) };
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27T14:30:15");

        model.Value.Should().Be(new DateTime(2026, 7, 27, 14, 30, 15));
    }

    [Fact]
    public void DateOnlyInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateOnly> { Value = new(2000, 1, 1) };
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27");

        model.Value.Should().Be(new DateOnly(2026, 7, 27));
    }

    [Fact]
    public void TimeOnlyInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<TimeOnly> { Value = new(1, 2, 3) };
        var cut = RenderInput(model);

        cut.Find("input").Change("14:30:15");

        model.Value.Should().Be(new TimeOnly(14, 30, 15));
    }

    [Fact]
    public void NullableDateTimeInput_WhenCleared_SetsModelToNull() {
        var model = new ValueModel<DateTime?> { Value = new(2026, 7, 27, 14, 30, 15) };
        var cut = RenderInput(model);

        cut.Find("input").Change(string.Empty);

        model.Value.Should().BeNull();
    }

    [Fact]
    public void NullableDateTimeInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateTime?>();
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27T14:30:15");

        model.Value.Should().Be(new DateTime(2026, 7, 27, 14, 30, 15));
    }

    [Fact]
    public void DateTimeOffsetInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateTimeOffset>();
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27T14:30:15+00:00");

        model.Value.Should().Be(new DateTimeOffset(2026, 7, 27, 14, 30, 15, TimeSpan.Zero));
    }

    [Fact]
    public void NullableDateTimeOffsetInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateTimeOffset?>();
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27T14:30:15+00:00");

        model.Value.Should().Be(new DateTimeOffset(2026, 7, 27, 14, 30, 15, TimeSpan.Zero));
    }

    [Fact]
    public void NullableDateOnlyInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<DateOnly?>();
        var cut = RenderInput(model);

        cut.Find("input").Change("2026-07-27");

        model.Value.Should().Be(new DateOnly(2026, 7, 27));
    }

    [Fact]
    public void NullableTimeOnlyInput_WithValidValue_UpdatesModel() {
        var model = new ValueModel<TimeOnly?>();
        var cut = RenderInput(model);

        cut.Find("input").Change("14:30:15");

        model.Value.Should().Be(new TimeOnly(14, 30, 15));
    }

    [Fact]
    public void DateTimeInput_WithMalformedValue_KeepsModelAndShowsExactValidationError() {
        var original = new DateTime(2026, 7, 27, 14, 30, 15);
        var model = new ValueModel<DateTime> { Value = original };
        var editContext = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, editContext)
            .Add(component => component.ChildContent, builder => {
                builder.OpenComponent<global::NTComponents.TnTInputDateTime<DateTime>>(0);
                builder.AddAttribute(1, nameof(global::NTComponents.TnTInputDateTime<DateTime>.ValueExpression), (Expression<Func<DateTime>>)(() => model.Value));
                builder.AddAttribute(2, nameof(global::NTComponents.TnTInputDateTime<DateTime>.Value), model.Value);
                builder.AddAttribute(3, nameof(global::NTComponents.TnTInputDateTime<DateTime>.ValueChanged), EventCallback.Factory.Create<DateTime>(this, value => model.Value = value));
                builder.CloseComponent();
            }));

        cut.Find("input").Change("not-a-date");

        model.Value.Should().Be(original);
        cut.Find(".tnt-validation-message").TextContent.Should().Be("Failed to parse not-a-date into a DateTime");
    }

    private IRenderedComponent<global::NTComponents.TnTInputDateTime<TValue>> RenderInput<TValue>(ValueModel<TValue> model) {
        return Render<global::NTComponents.TnTInputDateTime<TValue>>(parameters => parameters
            .Add(component => component.ValueExpression, () => model.Value)
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<TValue>(this, value => model.Value = value)));
    }

    private sealed class ValueModel<TValue> {
        public TValue Value { get; set; } = default!;
    }
}
