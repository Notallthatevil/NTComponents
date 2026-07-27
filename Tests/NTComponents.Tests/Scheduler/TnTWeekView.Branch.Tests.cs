using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Scheduler;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Scheduler;

public sealed class TnTWeekViewBranch_Tests : BunitContext {
    public TnTWeekViewBranch_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public async Task Refresh_Rebuilds_Visible_Events_After_Scheduler_State_Changes() {
        var scheduler = CreateScheduler([]);
        var cut = RenderWeekView(scheduler);
        cut.FindAll(".tnt-event").Should().BeEmpty();
        scheduler.Events.Add(CreateEvent("Added later", 9, 10));

        await cut.InvokeAsync(cut.Instance.Refresh);

        cut.Find(".tnt-event-title").TextContent.Should().Be("Added later");
    }

    [Fact]
    public void Empty_Slot_Click_Uses_Floored_Fifteen_Minute_Time() {
        DateTimeOffset? clicked = null;
        var scheduler = CreateScheduler([]);
        scheduler.EventSlotClickedCallback = EventCallback.Factory.Create<DateTimeOffset>(this, value => clicked = value);
        var cut = RenderWeekView(scheduler);

        cut.FindAll(".tnt-event-column")[3].Click(new MouseEventArgs { OffsetY = 61 });

        clicked.Should().NotBeNull();
        clicked!.Value.Date.Should().Be(new DateTime(2024, 6, 12));
        clicked.Value.TimeOfDay.Should().Be(TimeSpan.FromMinutes(75));
    }

    [Fact]
    public void Hovering_Empty_Column_Creates_And_Updates_Appointment_Placeholder() {
        var scheduler = CreateScheduler([]);
        var cut = RenderWeekView(scheduler, parameters => parameters.Add(p => p.DefaultAppointmentTime, TimeSpan.FromMinutes(45)));
        var eventColumns = cut.Find(".tnt-event-columns");

        eventColumns.MouseOver();
        cut.FindAll(".tnt-event-column")[3].MouseMove(new MouseEventArgs { OffsetY = 96 });

        var placeholder = cut.Find(".tnt-placeholder-event");
        placeholder.GetAttribute("style").Should().Contain("--tnt-event-start-hour:2;").And.Contain("--tnt-event-end-min:45;");
        placeholder.TextContent.Should().Contain("New Event");

        cut.FindAll(".tnt-event-column")[3].MouseMove(new MouseEventArgs { OffsetY = 120 });

        placeholder = cut.Find(".tnt-placeholder-event");
        placeholder.GetAttribute("style").Should().Contain("--tnt-event-start-min:30;").And.Contain("--tnt-event-end-hour:3;");
    }

    [Fact]
    public void Negative_Pointer_Offset_Does_Not_Create_A_Placeholder() {
        var scheduler = CreateScheduler([]);
        var cut = RenderWeekView(scheduler);
        cut.Find(".tnt-event-columns").MouseOver();

        cut.Find(".tnt-event-column").MouseMove(new MouseEventArgs { OffsetY = -1 });

        cut.FindAll(".tnt-placeholder-event").Should().BeEmpty();
    }

    [Fact]
    public void Drag_And_Click_State_Is_Visible_Through_Event_Classes_And_Callbacks() {
        var @event = CreateEvent("Interactive", 9, 10);
        TnTEvent? clicked = null;
        var scheduler = CreateScheduler([@event]);
        scheduler.EventClickedCallback = EventCallback.Factory.Create<TnTEvent>(this, value => clicked = value);
        var cut = RenderWeekView(scheduler);
        var eventElement = cut.Find(".tnt-event");

        eventElement.ClassList.Should().Contain("tnt-interactable");
        eventElement.Click();
        clicked.Should().BeSameAs(@event);
        eventElement.DragStart(new DragEventArgs());

        cut.Find(".tnt-event").ClassList.Should().Contain("tnt-dragging");

        cut.Find(".tnt-event").DragEnd(new DragEventArgs());

        cut.Find(".tnt-event").ClassList.Should().NotContain("tnt-dragging");
    }

    [Fact]
    public void Event_Spanning_The_Whole_Week_Is_Clipped_Into_Seven_Day_Segments() {
        var spanning = new TnTEvent {
            Title = "Long event",
            EventStart = new DateTimeOffset(2024, 6, 1, 9, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 30, 17, 0, 0, TimeSpan.Zero)
        };
        var cut = RenderWeekView(CreateScheduler([spanning]));

        var segments = cut.FindAll(".tnt-event");
        segments.Should().HaveCount(7);
        segments.Should().OnlyContain(segment => segment.QuerySelector(".tnt-event-title")!.TextContent == "Long event");
    }

    [Fact]
    public void Closely_Overlapping_Headers_Share_Equal_Columns() {
        var events = new List<TnTEvent> {
            CreateEvent("First", 9, 11),
            CreateEvent("Second", 9, 10, startMinute: 15),
            CreateEvent("Third", 9, 10, startMinute: 30)
        };
        var cut = RenderWeekView(CreateScheduler(events));

        var styles = cut.FindAll(".tnt-event").Select(element => element.GetAttribute("style")!).ToArray();
        styles.Should().Contain(style => style.Contains("left:0%;") && style.Contains("width:30%;"));
        styles.Should().Contain(style => style.Contains("left:30%;") && style.Contains("width:30%;"));
        styles.Should().Contain(style => style.Contains("left:60%;") && style.Contains("width:30%;"));
    }

    [Fact]
    public void Wider_Overlaps_Use_Progressive_Offsets() {
        var events = new List<TnTEvent> {
            CreateEvent("First", 9, 12),
            CreateEvent("Second", 10, 13)
        };
        var cut = RenderWeekView(CreateScheduler(events));

        var secondStyle = cut.FindAll(".tnt-event").Single(element => element.TextContent.Contains("Second", StringComparison.Ordinal)).GetAttribute("style");
        secondStyle.Should().Contain("left:2%;").And.Contain("width:88%;");
    }

    [Fact]
    public void Identical_Events_Are_Both_Retained_By_The_Sorted_View() {
        var events = new List<TnTEvent> {
            CreateEvent("Duplicate A", 9, 10),
            CreateEvent("Duplicate B", 9, 10)
        };
        var cut = RenderWeekView(CreateScheduler(events));

        cut.FindAll(".tnt-event").Should().HaveCount(2);
        cut.Markup.Should().Contain("Duplicate A").And.Contain("Duplicate B");
    }

    private IRenderedComponent<TnTWeekView<TnTEvent>> RenderWeekView(TnTScheduler<TnTEvent> scheduler, Action<ComponentParameterCollectionBuilder<TnTWeekView<TnTEvent>>>? configure = null) {
        return Render<TnTWeekView<TnTEvent>>(parameters => {
            parameters.AddCascadingValue(scheduler);
            configure?.Invoke(parameters);
        });
    }

    private static TnTScheduler<TnTEvent> CreateScheduler(List<TnTEvent> events) {
        return new TnTScheduler<TnTEvent> {
            Events = events,
            Date = new DateOnly(2024, 6, 12),
            EventClickedCallback = EventCallback<TnTEvent>.Empty,
            EventSlotClickedCallback = EventCallback<DateTimeOffset>.Empty,
            DateChangedCallback = EventCallback<DateOnly>.Empty
        };
    }

    private static TnTEvent CreateEvent(string title, int startHour, int endHour, DateOnly? date = null, int startMinute = 0) {
        var eventDate = date ?? new DateOnly(2024, 6, 12);
        return new TnTEvent {
            Title = title,
            EventStart = new DateTimeOffset(eventDate, new TimeOnly(startHour, startMinute), TimeSpan.Zero),
            EventEnd = new DateTimeOffset(eventDate, new TimeOnly(endHour, 0), TimeSpan.Zero)
        };
    }
}
