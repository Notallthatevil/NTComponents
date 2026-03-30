using System.Globalization;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Scheduler;

namespace NTComponents;

/// <summary>
///     Renders the week time-grid surface for <see cref="NTScheduler{TEventType}" />.
/// </summary>
/// <typeparam name="TEventType">The event type displayed by the scheduler.</typeparam>
public partial class NTSchedulerWeekView<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     The visible all-day spans.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NTSchedulerDateSpan<TEventType>> AllDaySpans { get; set; } = [];

    /// <summary>
    ///     Indicates whether event drag and drop is enabled.
    /// </summary>
    [Parameter]
    public bool CanDrag { get; set; }

    /// <summary>
    ///     Indicates whether time-grid slots can accept drop operations.
    /// </summary>
    [Parameter]
    public bool CanDrop { get; set; }

    /// <summary>
    ///     Indicates whether slot selection is enabled.
    /// </summary>
    [Parameter]
    public bool CanInteract { get; set; }

    /// <summary>
    ///     The visible day columns.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NTSchedulerDayColumn<TEventType>> Columns { get; set; } = [];

    /// <summary>
    ///     The current-time indicator position.
    /// </summary>
    [Parameter]
    public double? CurrentTimePercent { get; set; }

    /// <summary>
    ///     Raised when the user selects an event.
    /// </summary>
    [Parameter]
    public EventCallback<TEventType> OnEventSelected { get; set; }

    /// <summary>
    ///     Raised when the user selects a time slot.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerTimeSlot> OnSlotSelected { get; set; }

    /// <summary>
    ///     The visible time slots.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NTSchedulerTimeSlot> Slots { get; set; } = [];

    /// <summary>
    ///     The last visible hour.
    /// </summary>
    [Parameter]
    public int VisibleHourEnd { get; set; }

    /// <summary>
    ///     The first visible hour.
    /// </summary>
    [Parameter]
    public int VisibleHourStart { get; set; }

    private string GetColumnClass(NTSchedulerDayColumn<TEventType> column) {
        return CssClassBuilder.Create("nt-scheduler-week-view__day-column")
            .AddClass("nt-scheduler-week-view__day-column--today", column.IsToday)
            .Build() ?? string.Empty;
    }

    private static string GetAllDayEventClass(NTSchedulerDateSpan<TEventType> span) {
        return CssClassBuilder.Create("nt-scheduler-week-view__all-day-event")
            .AddClass("nt-scheduler-week-view__all-day-event--continues-before", span.ContinuesBefore)
            .AddClass("nt-scheduler-week-view__all-day-event--continues-after", span.ContinuesAfter)
            .Build() ?? string.Empty;
    }

    private static string GetAllDayEventStyle(NTSchedulerDateSpan<TEventType> span) {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"grid-column:{span.StartColumn + 1} / span {span.ColumnSpan};grid-row:{span.Lane + 1};");
    }

    private string GetAllDayGridStyle() {
        var visibleLaneCount = AllDaySpans.Count > 0 ? AllDaySpans.Max(span => span.Lane) + 1 : 1;
        return string.Create(CultureInfo.InvariantCulture, $"--nt-scheduler-all-day-lanes:{visibleLaneCount};");
    }

    private string GetEventStyle(NTSchedulerTimedEventLayout<TEventType> layout) {
        var totalMinutes = Math.Max(60, (VisibleHourEnd - VisibleHourStart) * 60.0);
        var eventStartMinutes = Math.Max(0, layout.SegmentStart.LocalDateTime.TimeOfDay.Subtract(TimeSpan.FromHours(VisibleHourStart)).TotalMinutes);
        var eventEndMinutes = Math.Min(totalMinutes, layout.SegmentEnd.LocalDateTime.TimeOfDay.Subtract(TimeSpan.FromHours(VisibleHourStart)).TotalMinutes);
        var topPercent = eventStartMinutes / totalMinutes * 100;
        var heightPercent = Math.Max(4, (eventEndMinutes - eventStartMinutes) / totalMinutes * 100);
        var leftPercent = layout.ColumnIndex * (100.0 / layout.ColumnCount);
        var widthPercent = 100.0 / layout.ColumnCount;

        return string.Create(CultureInfo.InvariantCulture, $"top:{topPercent:0.##}%;height:{heightPercent:0.##}%;left:{leftPercent:0.##}%;width:calc({widthPercent:0.##}% - 6px);");
    }

    private static string GetEventTime(TEventType @event) => $"{@event.EventStart.LocalDateTime:t} - {@event.EventEnd.LocalDateTime:t}";

    private static string GetEventAriaLabel(TEventType @event) => $"{@event.Title}, {@event.EventStart.LocalDateTime:g} to {@event.EventEnd.LocalDateTime:g}";

    private string GetHeaderClass(NTSchedulerDayColumn<TEventType> column) {
        return CssClassBuilder.Create("nt-scheduler-week-view__day-header")
            .AddClass("nt-scheduler-week-view__day-header--today", column.IsToday)
            .Build() ?? string.Empty;
    }

    private string GetCurrentTimeStyle() => $"top:{CurrentTimePercent.GetValueOrDefault().ToString("0.##", CultureInfo.InvariantCulture)}%;";

    private static string GetSlotLabel(NTSchedulerTimeSlot slot) => $"Create event at {slot.Start.LocalDateTime:f}";

    private string? GetSlotDisabledState() => CanInteract || CanDrop ? null : "true";

    private Task HandleSlotSelectedAsync(NTSchedulerTimeSlot slot) {
        return CanInteract ? OnSlotSelected.InvokeAsync(slot) : Task.CompletedTask;
    }
}
