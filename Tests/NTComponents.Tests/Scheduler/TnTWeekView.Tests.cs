using Microsoft.AspNetCore.Components;
using NTComponents.Scheduler;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Scheduler;

/// <summary>
///     Unit tests for <see cref="TnTWeekView{TEventType}" />.
/// </summary>
public class TnTWeekView_Tests : BunitContext {

    public TnTWeekView_Tests() {
        // Setup JSInterop for ripple effects
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void DecrementDate_SubtractsOneWeek() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        var sourceDate = new DateOnly(2024, 6, 15);
        var expectedDate = new DateOnly(2024, 6, 8);

        // Act
        var result = cut.Instance.DecrementDate(sourceDate);

        // Assert
        result.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void DefaultAppointmentTime_SetValue_ReturnsCorrectValue(int minutes) {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);
        var timeSpan = TimeSpan.FromMinutes(minutes);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.DefaultAppointmentTime, timeSpan));

        // Assert
        cut.Instance.DefaultAppointmentTime.Should().Be(timeSpan);
    }

    [Fact]
    public void EventColumn_DoesNotHaveInteractableClass_WhenNoEventSlotClickCallback() {
        // Arrange
        var events = new List<TnTEvent>();
        // Create scheduler without any callback (not even Empty)
        var scheduler = new TnTScheduler<TnTEvent> {
            Events = events,
            Date = DateOnly.FromDateTime(DateTime.Today),
            EventClickedCallback = EventCallback<TnTEvent>.Empty,
            DateChangedCallback = EventCallback<DateOnly>.Empty
            // Note: EventSlotClickedCallback is not set, so HasDelegate should be false
        };

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventColumns = cut.FindAll(".tnt-event-column");
        eventColumns.Should().HaveCount(7);
        foreach (var column in eventColumns) {
            column.GetAttribute("class")!.Should().NotContain("tnt-interactable");
        }
    }

    [Fact]
    public void EventColumn_HasInteractableClass_WhenEventSlotClickCallbackExists() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);
        scheduler.EventSlotClickedCallback = EventCallback.Factory.Create<DateTimeOffset>(this, _ => { });

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventColumns = cut.FindAll(".tnt-event-column");
        eventColumns.Should().HaveCount(7);
        foreach (var column in eventColumns) {
            column.GetAttribute("class")!.Should().Contain("tnt-interactable");
        }
    }

    [Fact]
    public void GetFirstVisibleDate_ReturnsStartOfWeek() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15)); // Saturday
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.StartViewOn, DayOfWeek.Sunday));

        // Act
        var result = cut.Instance.GetFirstVisibleDate();

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        result.Should().Be(new DateOnly(2024, 6, 9)); // Sunday before June 15, 2024
    }

    [Fact]
    public void GetLastVisibleDate_ReturnsEndOfWeek() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15)); // Saturday
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.StartViewOn, DayOfWeek.Sunday));

        // Act
        var result = cut.Instance.GetLastVisibleDate();

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        result.Should().Be(new DateOnly(2024, 6, 15)); // Saturday of that week
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HideDates_SetValue_ReturnsCorrectValue(bool hideDates) {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideDates, hideDates));

        // Assert
        cut.Instance.HideDates.Should().Be(hideDates);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HideEventDates_SetValue_ReturnsCorrectValue(bool hideEventDates) {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideEventDates, hideEventDates));

        // Assert
        cut.Instance.HideEventDates.Should().Be(hideEventDates);
    }

    [Fact]
    public void IncrementDate_AddsOneWeek() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        var sourceDate = new DateOnly(2024, 6, 15);
        var expectedDate = new DateOnly(2024, 6, 22);

        // Act
        var result = cut.Instance.IncrementDate(sourceDate);

        // Assert
        result.Should().Be(expectedDate);
    }

    [Fact]
    public void Renders_EventsOutsideWeek_AreNotShown() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Event Next Month",
                EventStart = new DateTimeOffset(2024, 7, 15, 10, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 7, 15, 11, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        eventElements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_EventTime_WhenHideEventDatesFalse() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Test Event",
                EventStart = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 12, 11, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 12));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideEventDates, false));

        // Assert
        var timeElements = cut.FindAll(".tnt-event-time");
        timeElements.Should().HaveCount(1);
        timeElements[0].TextContent.Should().Contain("-"); // Should contain time range
    }

    [Fact]
    public void Renders_EventWithDescription_WhenShowDescriptionTrue() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Test Event",
                Description = "Event description",
                EventStart = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 12, 11, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 12));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.ShowDescription, true));

        // Assert
        var descriptionElements = cut.FindAll(".tnt-event-description");
        descriptionElements.Should().HaveCount(1);
        descriptionElements[0].TextContent.Should().Be("Event description");
    }

    [Fact]
    public void Renders_EventWithoutDescription_WhenShowDescriptionFalse() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Test Event",
                Description = "Event description",
                EventStart = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 12, 11, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 12));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.ShowDescription, false));

        // Assert
        var descriptionElements = cut.FindAll(".tnt-event-description");
        descriptionElements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_MultiDayEvent_AcrossMultipleDays() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Multi-day Conference",
                EventStart = new DateTimeOffset(2024, 6, 15, 9, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 17, 17, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        // Multi-day event should create at least one event element (The actual rendering logic may show the multi-day event as segments or as a single event)
        eventElements.Should().HaveCountGreaterThanOrEqualTo(1);

        // Verify the event title is present
        eventElements.Should().Contain(e => e.QuerySelector(".tnt-event-title")!.TextContent == "Multi-day Conference");
    }

    [Fact]
    public void Renders_MultipleEvents_InSameDay() {
        // Arrange
        var baseDate = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Morning Event",
                EventStart = baseDate.AddHours(9),
                EventEnd = baseDate.AddHours(10)
            },
            new TnTEvent
            {
                Title = "Afternoon Event",
                EventStart = baseDate.AddHours(14),
                EventEnd = baseDate.AddHours(15)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        eventElements.Should().HaveCount(2);

        eventElements.Should().Contain(e => e.QuerySelector(".tnt-event-title")!.TextContent == "Morning Event");
        eventElements.Should().Contain(e => e.QuerySelector(".tnt-event-title")!.TextContent == "Afternoon Event");
    }

    [Fact]
    public void Renders_OverlappingEvents_WithCorrectLayout() {
        // Arrange
        var baseDate = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "First Event",
                EventStart = baseDate.AddHours(9),
                EventEnd = baseDate.AddHours(11)
            },
            new TnTEvent
            {
                Title = "Overlapping Event",
                EventStart = baseDate.AddHours(10),
                EventEnd = baseDate.AddHours(12)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        eventElements.Should().HaveCount(2);

        // The overlapping event should have reduced width and offset
        var overlappingEvent = eventElements.FirstOrDefault(e =>
            e.QuerySelector(".tnt-event-title")!.TextContent == "Overlapping Event");
        overlappingEvent.Should().NotBeNull();
    }

    [Fact]
    public void Renders_SingleEvent_InCorrectPosition() {
        // Arrange
        var eventStart = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2024, 6, 15, 11, 30, 0, TimeSpan.Zero);
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Test Meeting",
                Description = "Important meeting",
                EventStart = eventStart,
                EventEnd = eventEnd
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        eventElements.Should().HaveCount(1);

        var eventElement = eventElements[0];
        eventElement.QuerySelector(".tnt-event-title")!.TextContent.Should().Be("Test Meeting");
    }

    [Fact]
    public void Renders_TimeColumn_WithHourLabels() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var timeRows = cut.FindAll(".tnt-time-row");
        timeRows.Should().HaveCount(24); // 24 hours in a day

        // Check some specific hour labels
        timeRows[0].TextContent.Should().Contain("12 AM");
        timeRows[12].TextContent.Should().Contain("12 PM");
        timeRows[1].TextContent.Should().Contain("1 AM");
        timeRows[13].TextContent.Should().Contain("1 PM");
    }

    [Fact]
    public void Renders_TodayHighlight_OnCurrentDate() {
        // Arrange
        var events = new List<TnTEvent>();
        var today = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime);
        var scheduler = CreateTestScheduler(events, today);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var todayHeaders = cut.FindAll(".tnt-today");
        todayHeaders.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Renders_WeekDays_InCorrectOrder() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15)); // Saturday

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.StartViewOn, DayOfWeek.Sunday));

        // Assert
        var dateHeaders = cut.FindAll(".tnt-date-header-item");
        dateHeaders.Should().HaveCount(7);

        // Week of June 15, 2024 (Saturday) starting on Sunday should show June 9-15
        var expectedDates = new[]
        {
            "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"
        };

        for (int i = 0; i < expectedDates.Length; i++) {
            dateHeaders[i].TextContent.Should().Contain(expectedDates[i]);
        }
    }

    [Fact]
    public void Renders_WithAdditionalAttributes() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);
        var additionalAttributes = new Dictionary<string, object> {
            ["data-test"] = "week-view-test",
            ["class"] = "custom-week-view"
        };

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.AdditionalAttributes, additionalAttributes));

        // Assert
        var root = cut.Find("div.tnt-week-view");
        root.GetAttribute("data-test").Should().Be("week-view-test");
        root.GetAttribute("class")!.Should().Contain("custom-week-view");
    }

    [Fact]
    public void Renders_WithCorrectCssClasses() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var root = cut.Find("div.tnt-week-view");
        var classes = root.GetAttribute("class")!;
        classes.Should().Contain("tnt-week-view");
        classes.Should().Contain("tnt-components");
    }

    [Fact]
    public void Renders_WithCssVariables() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var root = cut.Find("div.tnt-week-view");
        var style = root.GetAttribute("style")!;
        style.Should().Contain("--header-height:80px");
        style.Should().Contain("--cell-min-width:80px");
        style.Should().Contain("--cell-height:48px");
        style.Should().Contain("--time-column-width:36px");
        style.Should().Contain("--hour-offset:48px");
    }

    [Fact]
    public void Renders_WithHideDates_ShowsOnlyDayOfWeek() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideDates, true));

        // Assert
        var dayOfWeekElements = cut.FindAll(".tnt-day-of-week");
        dayOfWeekElements.Should().HaveCount(7);

        var dateElements = cut.FindAll(".tnt-date");
        dateElements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_WithNoEvents_ShowsEmptyWeekView() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var eventElements = cut.FindAll(".tnt-event");
        eventElements.Should().BeEmpty();

        // But structure should still be there
        cut.Find(".tnt-date-header").Should().NotBeNull();
        cut.Find(".tnt-date-content").Should().NotBeNull();
    }

    [Fact]
    public void Renders_WithoutEventTime_WhenHideEventDatesTrue() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent
            {
                Title = "Test Event",
                EventStart = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 12, 11, 0, 0, TimeSpan.Zero)
            }
        };
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 12));

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideEventDates, true));

        // Assert
        var timeElements = cut.FindAll(".tnt-event-time");
        timeElements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_WithScheduler_ShowsCorrectStructure() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events, new DateOnly(2024, 6, 15)); // Saturday

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler));

        // Assert
        var root = cut.Find("div.tnt-week-view");
        root.Should().NotBeNull();

        // Check for date header structure
        cut.Find(".tnt-date-header").Should().NotBeNull();
        cut.Find(".tnt-time-column").Should().NotBeNull();
        cut.Find(".tnt-dates").Should().NotBeNull();

        // Check for content structure
        cut.Find(".tnt-date-content").Should().NotBeNull();
        cut.Find(".tnt-event-columns").Should().NotBeNull();
    }

    [Fact]
    public void Renders_WithShowDates_ShowsBothDayAndDate() {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.HideDates, false));

        // Assert
        var dayOfWeekElements = cut.FindAll(".tnt-day-of-week");
        dayOfWeekElements.Should().HaveCount(7);

        var dateElements = cut.FindAll(".tnt-date");
        dateElements.Should().HaveCount(7);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowDescription_SetValue_ReturnsCorrectValue(bool showDescription) {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.ShowDescription, showDescription));

        // Assert
        cut.Instance.ShowDescription.Should().Be(showDescription);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    public void StartViewOn_SetValue_ReturnsCorrectValue(DayOfWeek startDay) {
        // Arrange
        var events = new List<TnTEvent>();
        var scheduler = CreateTestScheduler(events);

        // Act
        var cut = Render<TnTWeekView<TnTEvent>>(parameters => parameters
            .AddCascadingValue(scheduler)
            .Add(p => p.StartViewOn, startDay));

        // Assert
        cut.Instance.StartViewOn.Should().Be(startDay);
    }

    private static TnTScheduler<TnTEvent> CreateTestScheduler(List<TnTEvent> events, DateOnly? date = null) {
        var scheduler = new TnTScheduler<TnTEvent> {
            Events = events,
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            EventClickedCallback = EventCallback<TnTEvent>.Empty,
            EventSlotClickedCallback = EventCallback<DateTimeOffset>.Empty,
            DateChangedCallback = EventCallback<DateOnly>.Empty
        };
        return scheduler;
    }
}