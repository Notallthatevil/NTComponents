using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Scheduler;
using NTComponents.Scheduler.Infrastructure;

namespace NTComponents.Tests.Scheduler;

public class ScheduleViewBase_Tests {

    [Fact]
    public void Ceiling_Floor_Round_BehaveAsExpected() {
        // Arrange
        var dt = new DateTimeOffset(2024, 1, 1, 10, 7, 0, TimeSpan.Zero);
        var interval = TimeSpan.FromMinutes(15);
        var view = new TestScheduleView();

        // Act
        var ceil = view.CallCeiling(dt, interval);
        var floor = view.CallFloor(dt, interval);
        var round = view.CallRound(dt, interval);

        // Assert
        ceil.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 15, 0, TimeSpan.Zero));
        floor.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
        // 10:07 rounds to 10:00 for 15min interval
        round.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Dispose_RemovesView_FromSchedulerDictionary() {
        // Arrange
        var view = new TestScheduleView();
        var scheduler = new TnTScheduler<TnTEvent> { Events = new List<TnTEvent>() };
        // add view via public method
        scheduler.AddScheduleView(view);

        var dictField = typeof(TnTScheduler<TnTEvent>).GetField("_scheduleViews", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var dict = (System.Collections.IDictionary)dictField.GetValue(scheduler)!;
        dict.Contains(view.GetType()).Should().BeTrue();

        // Act
        SetPrivateBackingField(view, "<Scheduler>k__BackingField", scheduler);
        view.Dispose();

        // Assert
        var dictAfter = (System.Collections.IDictionary)dictField.GetValue(scheduler)!;
        dictAfter.Contains(view.GetType()).Should().BeFalse();
    }

    [Fact]
    public async Task DragStart_SetsAndEnd_Clears_WhenAllowed() {
        // Arrange
        var view = new TestScheduleView();
        var scheduler = new TnTScheduler<TnTEvent> { AllowDraggingEvents = true, Events = new List<TnTEvent>() };
        SetPrivateBackingField(view, "<Scheduler>k__BackingField", scheduler);

        var ev = new TnTEvent { EventStart = DateTimeOffset.Now, EventEnd = DateTimeOffset.Now.AddHours(1) };
        var draggingField = typeof(ScheduleViewBase<TnTEvent>).GetField("<DraggingEvent>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - start drag
        await view.InvokeOnDragStartAsync(new DragEventArgs(), ev);

        // Assert - dragging was set
        var current = draggingField!.GetValue(view) as TnTEvent;
        current.Should().Be(ev);

        // Act - end drag
        await view.InvokeOnDragEndAsync(new DragEventArgs());

        // Assert - dragging cleared
        var after = draggingField.GetValue(view);
        after.Should().BeNull();
    }

    [Fact]
    public async Task EventClicked_And_SlotClicked_InvokeCallbacks() {
        // Arrange
        var view = new TestScheduleView();
        TnTEvent? clickedEvent = null;
        DateTimeOffset? clickedSlot = null;
        var scheduler = new TnTScheduler<TnTEvent> {
            Events = new List<TnTEvent>(),
            EventClickedCallback = EventCallback.Factory.Create<TnTEvent>(this, @event => clickedEvent = @event),
            EventSlotClickedCallback = EventCallback.Factory.Create<DateTimeOffset>(this, slot => clickedSlot = slot)
        };
        SetPrivateBackingField(view, "<Scheduler>k__BackingField", scheduler);
        var expectedEvent = new TnTEvent();
        var expectedSlot = new DateTimeOffset(2024, 1, 2, 9, 30, 0, TimeSpan.Zero);

        // Act
        await view.InvokeEventClickedAsync(expectedEvent);
        await view.InvokeEventSlotClickedAsync(expectedSlot);

        // Assert
        clickedEvent.Should().BeSameAs(expectedEvent);
        clickedSlot.Should().Be(expectedSlot);
    }

    [Fact]
    public async Task OnDrop_UpdatesEventTimes_And_RefreshIsCalled() {
        // Arrange
        var view = new TestScheduleView();
        var scheduler = new TnTScheduler<TnTEvent> { AllowDraggingEvents = true, Events = new List<TnTEvent>() };
        SetPrivateBackingField(view, "<Scheduler>k__BackingField", scheduler);

        var ev = new TnTEvent { EventStart = DateTimeOffset.Parse("2024-01-01T09:00:00Z"), EventEnd = DateTimeOffset.Parse("2024-01-01T10:00:00Z") };
        scheduler.Events.Add(ev);

        // set DraggingEvent backing field on view
        SetPrivateBackingField(view, "<DraggingEvent>k__BackingField", ev);

        var newStart = DateTimeOffset.Parse("2024-01-02T08:00:00Z");

        // Act
        await view.InvokeOnDropAsync(new DragEventArgs(), newStart);

        // Assert
        ev.EventStart.Should().Be(newStart);
        ev.EventEnd.Should().Be(newStart.Add(ev.Duration));
        view.RefreshedCalled.Should().BeTrue();
    }

    [Fact]
    public void OnInitialized_Throws_WhenSchedulerNull() {
        // Arrange
        var view = new TestScheduleView();

        // Act
        Action act = () => view.CallOnInitialized();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static void SetPrivateBackingField(object target, string backingFieldName, object? value) {
        var field = target.GetType().GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null) {
            // Try generic base type fields
            field = target.GetType().BaseType?.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        }
        field!.SetValue(target, value);
    }

    private class TestScheduleView : ScheduleViewBase<TnTEvent> {

        // Implement abstract properties from base component
        public override string? ElementClass => "test-schedule-view";

        public override string? ElementStyle => null;
        public DateOnly FirstVisible { get; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly LastVisible { get; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        public bool RefreshedCalled { get; private set; }

        public TestScheduleView() {
        }

        public DateTimeOffset CallCeiling(DateTimeOffset dateTime, TimeSpan interval) => Ceiling(dateTime, interval);

        public DateTimeOffset CallFloor(DateTimeOffset dateTime, TimeSpan interval) => Floor(dateTime, interval);

        public void CallOnInitialized() => OnInitialized();

        public DateTimeOffset CallRound(DateTimeOffset dateTime, TimeSpan interval) => Round(dateTime, interval);

        public override DateOnly DecrementDate(DateOnly src) => src.AddDays(-1);

        public new void Dispose() => base.Dispose();

        public override DateOnly GetFirstVisibleDate() => FirstVisible;

        public override DateOnly GetLastVisibleDate() => LastVisible;

        public override DateOnly IncrementDate(DateOnly src) => src.AddDays(1);

        public Task InvokeEventClickedAsync(TnTEvent? e) => EventClickedAsync(e);

        public Task InvokeEventSlotClickedAsync(DateTimeOffset slot) => EventSlotClickedAsync(slot);

        public Task InvokeOnDragEndAsync(DragEventArgs args) => OnDragEndAsync(args);

        public Task InvokeOnDragStartAsync(DragEventArgs args, TnTEvent ev) => OnDragStartAsync(args, ev);

        public Task InvokeOnDropAsync(DragEventArgs args, DateTimeOffset start) => OnDropAsync(args, start);

        public override void Refresh() => RefreshedCalled = true;

        protected override DateTimeOffset CalculateDateTimeOffset(double pointerYOffset, DateOnly date) => date.ToDateTime(new TimeOnly(0), DateTimeKind.Utc);
    }
}
