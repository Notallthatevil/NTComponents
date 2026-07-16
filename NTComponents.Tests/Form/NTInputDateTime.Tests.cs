using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public class NTInputDateTime_Tests : BunitContext {

    private sealed class DateOnlyModel {
        public DateOnly? Value { get; set; }
    }

    private sealed class DateTimeModel {
        public DateTime? Value { get; set; }
    }

    private sealed class DateTimeOffsetModel {
        public DateTimeOffset? Value { get; set; }
    }

    private sealed class TimeOnlyModel {
        public TimeOnly? Value { get; set; }
    }

    [Fact]
    public void DateTime_Renders_DateTimeLocal_Input_And_Default_Format() {
        var cut = RenderDateTime();

        cut.Find("input").GetAttribute("type").Should().Be("datetime-local");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
        cut.Instance.Type.Should().Be(InputType.DateTime);
        cut.Instance.Format.Should().BeNull();
    }

    [Fact]
    public void DateTime_Uses_Custom_Format_Without_Mutating_Parameter() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 10, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters.Add(p => p.Format, "yyyy-MM-ddTHH:mm"));

        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm");
        cut.Find("input").GetAttribute("value").Should().Be("2026-05-19T10:30");
        cut.Instance.Format.Should().Be("yyyy-MM-ddTHH:mm");
    }

    [Fact]
    public void DateTime_Custom_Picker_Uses_Effective_Format_Attribute() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 10, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Format, "yyyy-MM-ddTHH:mm"));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm");
    }

    [Fact]
    public void DateTime_Custom_Picker_Uses_Text_Input_For_NonNative_Format() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 14, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Format, "MM/dd/yyyy hh:mm tt"));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("text");
        input.GetAttribute("value").Should().Be("05/19/2026 02:30 PM");
    }

    [Fact]
    public void DateTimeOffset_Renders_DateTimeLocal_Input_And_Default_Format() {
        var model = new DateTimeOffsetModel {
            Value = new DateTimeOffset(2026, 5, 19, 10, 30, 0, TimeSpan.Zero)
        };

        var cut = RenderDateTimeOffset(model);

        cut.Find("input").GetAttribute("type").Should().Be("datetime-local");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
        cut.Find("input").GetAttribute("value").Should().Be("2026-05-19T10:30:00");
        cut.Instance.Type.Should().Be(InputType.DateTime);
    }

    [Fact]
    public void DateOnly_Renders_Date_Input() {
        var cut = RenderDateOnly();

        cut.Find("input").GetAttribute("type").Should().Be("date");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-dd");
        cut.Instance.Type.Should().Be(InputType.Date);
    }

    [Fact]
    public void DateOnly_MonthOnly_Renders_Month_Input() {
        var cut = RenderDateOnly(configure: parameters => parameters.Add(p => p.MonthOnly, true));

        cut.Find("input").GetAttribute("type").Should().Be("month");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM");
        cut.Instance.Type.Should().Be(InputType.Month);
    }

    [Fact]
    public void TimeOnly_Renders_Time_Input() {
        var cut = RenderTimeOnly();

        cut.Find("input").GetAttribute("type").Should().Be("time");
        cut.Find("input").GetAttribute("format").Should().Be("HH:mm:ss");
        cut.Instance.Type.Should().Be(InputType.Time);
    }

    [Fact]
    public void DateTime_Change_Updates_Value() {
        var model = new DateTimeModel();
        var cut = RenderDateTime(model);

        cut.Find("input").Change("2026-05-19T10:30:00");

        model.Value.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
    }

    [Fact]
    public void Unsupported_Type_Throws() {
        var model = new { Value = "" };

        var act = () => Render<NTInputDateTime<string>>(parameters => parameters
            .Add(p => p.Value, model.Value)
            .Add(p => p.ValueExpression, () => model.Value));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The type 'System.String' is not a supported DateTime type.");
    }

    [Fact]
    public void Custom_Picker_Markup_Is_Not_Rendered_By_Default() {
        var cut = RenderDateTime();

        cut.FindAll("[data-tnt-dtp-picker='true']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-trigger='true']").Should().BeEmpty();
    }

    [Fact]
    public void Native_Input_Type_Is_Used_By_Default() {
        var cut = RenderDateOnly();

        cut.Find("input").GetAttribute("type").Should().Be("date");
    }

    [Fact]
    public void DateTime_Custom_Picker_Renders_DateTime_Mode() {
        var cut = RenderDateTime(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("datetime-local");
        input.GetAttribute("data-tnt-dtp-mode").Should().Be("datetime");
        var pickerId = input.GetAttribute("data-tnt-dtp-target");
        pickerId.Should().NotBeNullOrWhiteSpace();

        cut.Find($"button[data-tnt-dtp-trigger='true'][data-tnt-dtp-target='{pickerId}']")
            .GetAttribute("aria-label")
            .Should()
            .Be("Open date and time picker");
        cut.Find($"div#{pickerId}[data-tnt-dtp-picker='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("datetime");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Date and time");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-label]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-calendar-year-label]").Should().BeEmpty();
        cut.FindAll(".tnt-dtp-menu-button-icon").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-second]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='today']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='now']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='clear']").Should().BeEmpty();
        cut.Find("[data-tnt-dtp-action='cancel']").TextContent.Should().Be("Cancel");
        cut.Find("[data-tnt-dtp-action='confirm']").TextContent.Should().Be("OK");
    }

    [Fact]
    public void Date_Custom_Picker_Renders_Date_Mode() {
        var cut = RenderDateOnly(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("date");
        input.GetAttribute("data-tnt-dtp-mode").Should().Be("date");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-date");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Date");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
    }

    [Fact]
    public void Month_Custom_Picker_Renders_Month_Mode_Only() {
        var cut = RenderDateOnly(configure: parameters => parameters
            .Add(p => p.MonthOnly, true)
            .Add(p => p.EnableCustomPicker, true));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("month");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-month");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Month");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
    }

    [Fact]
    public void Time_Custom_Picker_Renders_Time_Mode_And_Clock_Trigger() {
        var cut = RenderTimeOnly(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("time");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-time");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Time");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-second]").Should().BeEmpty();
        cut.Markup.Should().Contain("schedule");
    }

    private IRenderedComponent<NTInputDateTime<DateOnly?>> RenderDateOnly(DateOnlyModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateOnly?>>>? configure = null) {
        model ??= new DateOnlyModel();
        return Render<NTInputDateTime<DateOnly?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<DateTime?>> RenderDateTime(DateTimeModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateTime?>>>? configure = null) {
        model ??= new DateTimeModel();
        return Render<NTInputDateTime<DateTime?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<DateTimeOffset?>> RenderDateTimeOffset(DateTimeOffsetModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateTimeOffset?>>>? configure = null) {
        model ??= new DateTimeOffsetModel();
        return Render<NTInputDateTime<DateTimeOffset?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<TimeOnly?>> RenderTimeOnly(TimeOnlyModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<TimeOnly?>>>? configure = null) {
        model ??= new TimeOnlyModel();
        return Render<NTInputDateTime<TimeOnly?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<TimeOnly?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }
}
