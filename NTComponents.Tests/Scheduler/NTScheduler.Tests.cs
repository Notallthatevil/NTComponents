using Microsoft.AspNetCore.Components;
using NTComponents.Scheduler;

namespace NTComponents.Tests.Scheduler;

/// <summary>
///     Unit tests for <see cref="NTScheduler{TEventType}" />.
/// </summary>
public class NTScheduler_Tests : BunitContext {

    public NTScheduler_Tests() {
        var schedulerModule = JSInterop.SetupModule("./_content/NTComponents/Scheduler/NTScheduler.razor.js");
        schedulerModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        schedulerModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        schedulerModule.SetupVoid("onDispose", _ => true).SetVoidResult();

        SetRendererInfo(new RendererInfo("WebAssembly", true));
    }

    [Fact]
    public void DefaultRender_ShowsMonthViewWithMonthCells() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events));

        // Assert
        cut.Find(".nt-scheduler-month-view").Should().NotBeNull();
        cut.FindAll(".nt-scheduler-month-view__cell").Should().HaveCount(42);
    }

    [Fact]
    public void WhenViewButtonClicked_UpdatesViewAndInvokesCallback() {
        // Arrange
        var events = new List<TnTEvent>();
        var callbackValue = NTSchedulerView.Month;

        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.ViewChanged, EventCallback.Factory.Create<NTSchedulerView>(this, view => callbackValue = view)));

        // Act
        cut.FindAll(".nt-scheduler__view-button").Single(button => button.TextContent.Contains("Week")).Click();

        // Assert
        cut.Instance.View.Should().Be(NTSchedulerView.Week);
        callbackValue.Should().Be(NTSchedulerView.Week);
        cut.Find(".nt-scheduler-week-view").Should().NotBeNull();
    }

    [Fact]
    public void WhenMonthCellSelected_OpensEditorAndSavingCreatesEvent() {
        // Arrange
        var events = new List<TnTEvent>();
        NTSchedulerChangeKind? changeKind = null;

        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.EventChanged, EventCallback.Factory.Create<NTSchedulerEventChange<TnTEvent>>(this, args => changeKind = args.Kind)));

        // Act
        cut.FindAll(".nt-scheduler-month-view__cell-hit-target")[10].Click();
        cut.Find("input[type='text']").Change("Sprint planning");
        cut.FindAll("input[type='datetime-local']")[0].Change("2026-03-12T09:00");
        cut.FindAll("input[type='datetime-local']")[1].Change("2026-03-12T10:00");
        cut.FindAll(".nt-scheduler-event-editor__button").Single(button => button.TextContent.Contains("Save")).Click();

        // Assert
        events.Should().HaveCount(1);
        events[0].Title.Should().Be("Sprint planning");
        changeKind.Should().Be(NTSchedulerChangeKind.Created);
    }

    [Fact]
    public void WhenEventSelected_DeleteRemovesEvent() {
        // Arrange
        var events = new List<TnTEvent> {
            new() {
                Title = "Review",
                EventStart = new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.FromHours(-6)),
                EventEnd = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.FromHours(-6))
            }
        };

        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events));

        // Act
        cut.Find(".nt-scheduler-month-view__event").Click();
        cut.FindAll(".nt-scheduler-event-editor__button").Single(button => button.TextContent.Contains("Delete")).Click();

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenHandleJsDropAsyncCalled_MovesEventAndInvokesMovedCallback() {
        // Arrange
        var originalStart = new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.FromHours(-6));
        var events = new List<TnTEvent> {
            new() {
                Title = "Standup",
                EventStart = originalStart,
                EventEnd = originalStart.AddHours(1)
            }
        };

        NTSchedulerEventChange<TnTEvent>? capturedChange = null;
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.EventChanged, EventCallback.Factory.Create<NTSchedulerEventChange<TnTEvent>>(this, args => capturedChange = args)));

        var updatedStart = originalStart.AddDays(1).AddHours(2);

        // Act
        await cut.InvokeAsync(() => cut.Instance.HandleJsDropAsync(events[0].Id.ToString(), updatedStart.ToString("O")));

        // Assert
        events[0].EventStart.Should().Be(updatedStart);
        events[0].EventEnd.Should().Be(updatedStart.AddHours(1));
        capturedChange.Should().NotBeNull();
        capturedChange!.Kind.Should().Be(NTSchedulerChangeKind.Moved);
        capturedChange.PreviousStart.Should().Be(originalStart);
    }

    [Fact]
    public void WhenRendererIsStatic_DisablesInteractiveControls() {
        // Arrange
        SetRendererInfo(new RendererInfo("Static", false));
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events));

        // Assert
        cut.FindAll(".nt-scheduler__icon-button").Should().OnlyContain(button => button.HasAttribute("disabled"));
        cut.FindAll(".nt-scheduler-month-view__cell-hit-target").Should().OnlyContain(button => button.HasAttribute("disabled"));
    }

    [Fact]
    public void WhenWeekViewRendered_RendersSevenDayColumns() {
        // Arrange
        var events = new List<TnTEvent>();

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.View, NTSchedulerView.Week));

        // Assert
        cut.Find(".nt-scheduler-week-view__day-columns").Children.Should().HaveCount(7);
        cut.FindAll(".nt-scheduler-week-view__day-column").Should().HaveCount(7);
    }

    [Fact]
    public void WhenDragDropEnabledWithoutCreation_WeekSlotsRemainAvailableForDrop() {
        // Arrange
        var events = new List<TnTEvent> {
            new() {
                Title = "Standup",
                EventStart = new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.FromHours(-6)),
                EventEnd = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.FromHours(-6))
            }
        };

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.EnableDragDrop, true)
            .Add(parameter => parameter.EnableEventCreation, false)
            .Add(parameter => parameter.EnableEventEditing, false)
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.View, NTSchedulerView.Week));

        // Assert
        cut.FindAll(".nt-scheduler-week-view__slot").Should().NotBeEmpty();
        cut.FindAll(".nt-scheduler-week-view__slot").Should().OnlyContain(slot => !slot.HasAttribute("disabled"));
    }

    [Fact]
    public void WhenMonthEventSpansMultipleDays_RendersSingleContinuousMonthItem() {
        // Arrange
        var events = new List<TnTEvent> {
            new() {
                Title = "Conference trip",
                EventStart = new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.FromHours(-6)),
                EventEnd = new DateTimeOffset(2026, 3, 12, 17, 0, 0, TimeSpan.FromHours(-6))
            }
        };

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 20))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.View, NTSchedulerView.Month));

        // Assert
        cut.FindAll(".nt-scheduler-month-view__event").Should().HaveCount(1);
        cut.Find(".nt-scheduler-month-view__event").GetAttribute("style").Should().Contain("span 3");
    }

    [Fact]
    public void WhenWeekEventSpansMultipleDays_RendersSingleContinuousAllDayItem() {
        // Arrange
        var events = new List<TnTEvent> {
            new() {
                Title = "Conference trip",
                EventStart = new DateTimeOffset(2026, 3, 16, 9, 0, 0, TimeSpan.FromHours(-6)),
                EventEnd = new DateTimeOffset(2026, 3, 18, 17, 0, 0, TimeSpan.FromHours(-6))
            }
        };

        // Act
        var cut = Render<NTScheduler<TnTEvent>>(parameters => parameters
            .Add(parameter => parameter.Date, new DateOnly(2026, 3, 18))
            .Add(parameter => parameter.Events, events)
            .Add(parameter => parameter.View, NTSchedulerView.Week));

        // Assert
        cut.FindAll(".nt-scheduler-week-view__all-day-event").Should().HaveCount(1);
        cut.Find(".nt-scheduler-week-view__all-day-event").GetAttribute("style").Should().Contain("span 3");
    }
}
