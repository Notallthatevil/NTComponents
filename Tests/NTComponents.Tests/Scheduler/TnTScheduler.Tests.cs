using Microsoft.AspNetCore.Components;
using NTComponents.Scheduler;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Scheduler;

/// <summary>
///     Unit tests for <see cref="TnTScheduler{TEventType}" />.
/// </summary>
public class TnTScheduler_Tests : BunitContext {

    public TnTScheduler_Tests() {
        // Setup JSInterop for ripple effects
        RippleTestingUtility.SetupRippleEffectModule(this);
        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void AddScheduleView_AddsViewToScheduler() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Create a mock schedule view
        var mockView = new MockScheduleView<TnTEvent>();

        // Act
        cut.Instance.AddScheduleView(mockView);

        // Assert
        cut.Instance.IsViewSelected(mockView).Should().BeTrue(); // First view becomes selected
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllowDraggingEvents_SetValue_ReturnsCorrectValue(bool allowDragging) {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.AllowDraggingEvents, allowDragging));

        // Assert
        cut.Instance.AllowDraggingEvents.Should().Be(allowDragging);
    }

    [Theory]
    [InlineData(TnTColor.Primary)]
    [InlineData(TnTColor.Secondary)]
    [InlineData(TnTColor.Success)]
    [InlineData(TnTColor.Error)]
    public void BackgroundColor_SetValue_ReturnsCorrectValue(TnTColor color) {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.BackgroundColor, color));

        // Assert
        cut.Instance.BackgroundColor.Should().Be(color);
    }

    [Fact]
    public void ChildContent_RendersWithCascadingValue() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .AddChildContent("<div class=\"test-child\">Test Content</div>"));

        // Assert
        cut.Find(".test-child").Should().NotBeNull();
        cut.Markup.Should().Contain("Test Content");
    }

    [Fact]
    public void Events_SetValue_ReturnsCorrectValue() {
        // Arrange
        var events = new List<TnTEvent>
        {
            new TnTEvent { Title = "Event 1" },
            new TnTEvent { Title = "Event 2" }
        };

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Instance.Events.Should().BeEquivalentTo(events);
    }

    [Fact]
    public void GetFirstVisibleDate_WithNoView_ReturnsNull() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Act
        var result = cut.Instance.GetFirstVisibleDate();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFirstVisibleDate_WithView_ReturnsViewDate() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        var mockView = new MockScheduleView<TnTEvent>();
        var expectedDate = new DateOnly(2024, 6, 15);
        mockView.FirstVisibleDate = expectedDate;
        cut.Instance.AddScheduleView(mockView);

        // Act
        var result = cut.Instance.GetFirstVisibleDate();

        // Assert
        result.Should().Be(expectedDate);
    }

    [Fact]
    public void GetLastVisibleDate_WithNoView_ReturnsNull() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Act
        var result = cut.Instance.GetLastVisibleDate();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLastVisibleDate_WithView_ReturnsViewDate() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        var mockView = new MockScheduleView<TnTEvent>();
        var expectedDate = new DateOnly(2024, 6, 21);
        mockView.LastVisibleDate = expectedDate;
        cut.Instance.AddScheduleView(mockView);

        // Act
        var result = cut.Instance.GetLastVisibleDate();

        // Assert
        result.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData(TnTDayOfWeekFlag.Sunday, DayOfWeek.Sunday, true)]
    [InlineData(TnTDayOfWeekFlag.Monday, DayOfWeek.Monday, true)]
    [InlineData(TnTDayOfWeekFlag.Sunday, DayOfWeek.Monday, false)]
    [InlineData(TnTDayOfWeekFlag.All, DayOfWeek.Wednesday, true)]
    [InlineData(TnTDayOfWeekFlag.Monday | TnTDayOfWeekFlag.Friday, DayOfWeek.Friday, true)]
    [InlineData(TnTDayOfWeekFlag.Monday | TnTDayOfWeekFlag.Friday, DayOfWeek.Tuesday, false)]
    public void HasDay_ChecksDayCorrectly(TnTDayOfWeekFlag flag, DayOfWeek dayOfWeek, bool expected) {
        // Act
        var result = flag.HasDay(dayOfWeek);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(TnTColor.Primary)]
    [InlineData(TnTColor.Secondary)]
    [InlineData(TnTColor.Info)]
    public void PlaceholderBackgroundColor_SetValue_ReturnsCorrectValue(TnTColor color) {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.PlaceholderBackgroundColor, color));

        // Assert
        cut.Instance.PlaceholderBackgroundColor.Should().Be(color);
    }

    [Theory]
    [InlineData(TnTColor.OnPrimary)]
    [InlineData(TnTColor.OnSecondary)]
    [InlineData(TnTColor.OnInfo)]
    public void PlaceholderTextColor_SetValue_ReturnsCorrectValue(TnTColor color) {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.PlaceholderTextColor, color));

        // Assert
        cut.Instance.PlaceholderTextColor.Should().Be(color);
    }

    [Fact]
    public async Task Refresh_CallsStateHasChanged() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.ChildContent, new RenderFragment(b => {
                b.OpenComponent<MockScheduleView<TnTEvent>>(0);
                b.CloseComponent();
            })));

        var mockView = cut.FindComponent<MockScheduleView<TnTEvent>>();
        // Act

        await cut.InvokeAsync(cut.Instance.Refresh);

        // Assert
        mockView.Should().NotBeNull();
        mockView.RenderCount.Should().Be(2);
    }

    [Fact]
    public void RemoveScheduleView_RemovesViewFromScheduler() {
        // Arrange
        var events = new List<TnTEvent>();
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        var mockView1 = new MockScheduleView<TnTEvent>();
        var mockView2 = new MockScheduleView<TnTEvent>();
        cut.Instance.AddScheduleView(mockView1);
        cut.Instance.AddScheduleView(mockView2);

        // Act
        cut.Instance.RemoveScheduleView(mockView1);

        // Assert mockView2 should still be selected since it's still in the scheduler
        cut.Instance.IsViewSelected(mockView2).Should().BeTrue();
        cut.Instance.IsViewSelected(mockView1).Should().BeFalse();
    }

    [Fact]
    public void Renders_DateControls_ByDefault() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        cut.Find(".date-controls").Should().NotBeNull();
        cut.Find(".prev-button").Should().NotBeNull();
        cut.Find(".today-button").Should().NotBeNull();
        cut.Find(".next-button").Should().NotBeNull();
    }

    [Fact]
    public void Renders_WithAdditionalAttributes_MergesAttributes() {
        // Arrange
        var events = new List<TnTEvent>();
        var additionalAttributes = new Dictionary<string, object> {
            ["data-test"] = "scheduler-test",
            ["class"] = "custom-class"
        };

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.AdditionalAttributes, additionalAttributes));

        // Assert
        var root = cut.Find("div.tnt-scheduler");
        root.GetAttribute("data-test").Should().Be("scheduler-test");
        root.GetAttribute("class")!.Should().Contain("custom-class");
    }

    [Fact]
    public void Renders_WithCustomColors_AppliesCorrectClasses() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.BackgroundColor, TnTColor.Primary)
            .Add(p => p.TextColor, TnTColor.OnPrimary));

        // Assert
        var root = cut.Find("div.tnt-scheduler");
        var classes = root.GetAttribute("class")!;
        classes.Should().Contain("tnt-bg-color-primary");
        classes.Should().Contain("tnt-fg-color-on-primary");
    }

    [Fact]
    public void Renders_WithCustomDate_SetsDateProperty() {
        // Arrange
        var events = new List<TnTEvent>();
        var customDate = new DateOnly(2024, 6, 15);

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, customDate));

        // Assert
        cut.Instance.Date.Should().Be(customDate);
        cut.Instance.DayOfWeek.Should().Be(customDate.DayOfWeek);
    }

    [Fact]
    public void Renders_WithDefaultValues_SetsCorrectClassesAndStructure() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events));

        // Assert
        var root = cut.Find("div.tnt-scheduler");
        root.Should().NotBeNull();

        var classes = root.GetAttribute("class")!;
        classes.Should().Contain("tnt-scheduler");
        classes.Should().Contain("tnt-components");
        classes.Should().Contain("tnt-filled");
        classes.Should().Contain("tnt-bg-color-surface-container-low");
        classes.Should().Contain("tnt-fg-color-on-surface");
    }

    [Fact]
    public void Renders_WithHideDateControls_HidesDateControls() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.HideDateControls, true));

        // Assert
        cut.FindAll(".date-controls").Should().BeEmpty();
    }

    [Theory]
    [InlineData(TnTColor.OnPrimary)]
    [InlineData(TnTColor.OnSecondary)]
    [InlineData(TnTColor.OnSuccess)]
    [InlineData(TnTColor.OnError)]
    public void TextColor_SetValue_ReturnsCorrectValue(TnTColor color) {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.TextColor, color));

        // Assert
        cut.Instance.TextColor.Should().Be(color);
    }

    [Fact]
    public void TodayButton_Click_InvokesDateChangedCallback() {
        // Arrange
        var events = new List<TnTEvent>();
        var dateChangedCallbackInvoked = false;
        DateOnly changedDate = default;

        // Act
        var cut = Render<TnTScheduler<TnTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateOnly(2024, 1, 15))
            .Add(p => p.DateChangedCallback, EventCallback.Factory.Create<DateOnly>(this, date => {
                dateChangedCallbackInvoked = true;
                changedDate = date;
            })));

        cut.Find(".today-button").Click();

        // Assert
        dateChangedCallbackInvoked.Should().BeTrue();
        changedDate.Should().Be(DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, TnTDayOfWeekFlag.Sunday)]
    [InlineData(DayOfWeek.Monday, TnTDayOfWeekFlag.Monday)]
    [InlineData(DayOfWeek.Tuesday, TnTDayOfWeekFlag.Tuesday)]
    [InlineData(DayOfWeek.Wednesday, TnTDayOfWeekFlag.Wednesday)]
    [InlineData(DayOfWeek.Thursday, TnTDayOfWeekFlag.Thursday)]
    [InlineData(DayOfWeek.Friday, TnTDayOfWeekFlag.Friday)]
    [InlineData(DayOfWeek.Saturday, TnTDayOfWeekFlag.Saturday)]
    public void ToTnTDayOfWeekFlag_ConvertsDayOfWeekCorrectly(DayOfWeek dayOfWeek, TnTDayOfWeekFlag expected) {
        // Act
        var result = dayOfWeek.ToTnTDayOfWeekFlag();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToTnTDayOfWeekFlag_WithInvalidDayOfWeek_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var invalidDayOfWeek = (DayOfWeek)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidDayOfWeek.ToTnTDayOfWeekFlag());
    }

    private class MockScheduleView<TEventType> : NTComponents.Scheduler.Infrastructure.ScheduleViewBase<TEventType>
        where TEventType : TnTEvent {
        public override string? ElementClass => "mock-schedule-view";
        public override string? ElementStyle => null;
        public DateOnly FirstVisibleDate { get; set; } = new DateOnly(2024, 1, 1);
        public DateOnly LastVisibleDate { get; set; } = new DateOnly(2024, 1, 7);
        public bool RefreshCalled { get; private set; }

        public override DateOnly DecrementDate(DateOnly src) => src.AddDays(-7);

        public override DateOnly GetFirstVisibleDate() => FirstVisibleDate;

        public override DateOnly GetLastVisibleDate() => LastVisibleDate;

        public override DateOnly IncrementDate(DateOnly src) => src.AddDays(7);

        public override void Refresh() {
            RefreshCalled = true;
            StateHasChanged();
        }

        protected override DateTimeOffset CalculateDateTimeOffset(double pointerYOffset, DateOnly date) {
            return new DateTimeOffset(date, TimeOnly.MinValue, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow));
        }
    }
}