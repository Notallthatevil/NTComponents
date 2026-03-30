using System.Globalization;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Scheduler;

namespace NTComponents;

/// <summary>
///     Renders the month-grid surface for <see cref="NTScheduler{TEventType}" />.
/// </summary>
/// <typeparam name="TEventType">The event type displayed by the scheduler.</typeparam>
public partial class NTSchedulerMonthView<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Indicates whether event drag and drop is enabled.
    /// </summary>
    [Parameter]
    public bool CanDrag { get; set; }

    /// <summary>
    ///     Indicates whether empty day cells can accept drop operations.
    /// </summary>
    [Parameter]
    public bool CanDrop { get; set; }

    /// <summary>
    ///     Indicates whether empty day selection is enabled.
    /// </summary>
    [Parameter]
    public bool CanInteract { get; set; }

    /// <summary>
    ///     The visible month rows.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NTSchedulerMonthRow<TEventType>> Rows { get; set; } = [];

    /// <summary>
    ///     The maximum number of visible event chips rendered per day cell.
    /// </summary>
    [Parameter]
    public int MaxVisibleEventsPerDay { get; set; } = 3;

    /// <summary>
    ///     Raised when the user selects a day cell.
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly> OnDaySelected { get; set; }

    /// <summary>
    ///     Raised when the user selects an event.
    /// </summary>
    [Parameter]
    public EventCallback<TEventType> OnEventSelected { get; set; }

    /// <summary>
    ///     The weekday labels rendered in the header row.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> WeekdayLabels { get; set; } = [];

    private static string GetEventAriaLabel(TEventType @event) {
        var sameDay = @event.StartDate == @event.EndDate;
        var timeRange = sameDay
            ? $"{@event.EventStart.LocalDateTime:t} to {@event.EventEnd.LocalDateTime:t}"
            : $"{@event.EventStart.LocalDateTime:g} to {@event.EventEnd.LocalDateTime:g}";

        return $"{@event.Title}, {timeRange}";
    }

    private static string GetCellClass(NTSchedulerMonthCell<TEventType> cell) {
        return CssClassBuilder.Create("nt-scheduler-month-view__cell")
            .AddClass("nt-scheduler-month-view__cell--outside", !cell.IsCurrentMonth)
            .AddClass("nt-scheduler-month-view__cell--today", cell.IsToday)
            .Build() ?? string.Empty;
    }

    private static string GetSpanClass(NTSchedulerDateSpan<TEventType> span) {
        return CssClassBuilder.Create("nt-scheduler-month-view__event")
            .AddClass("nt-scheduler-month-view__event--continues-before", span.ContinuesBefore)
            .AddClass("nt-scheduler-month-view__event--continues-after", span.ContinuesAfter)
            .Build() ?? string.Empty;
    }

    private string? GetInteractiveState(NTSchedulerMonthCell<TEventType> _) {
        return CanInteract || CanDrop ? null : "true";
    }

    private static string GetSpanStyle(NTSchedulerDateSpan<TEventType> span) {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"grid-column:{span.StartColumn + 1} / span {span.ColumnSpan};grid-row:{span.Lane + 1};");
    }

    private static string GetWeekStyle(NTSchedulerMonthRow<TEventType> row) {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"--nt-scheduler-month-visible-lanes:{Math.Max(0, row.VisibleLaneCount)};");
    }

    private Task HandleDaySelectedAsync(DateOnly date) {
        return CanInteract ? OnDaySelected.InvokeAsync(date) : Task.CompletedTask;
    }
}
