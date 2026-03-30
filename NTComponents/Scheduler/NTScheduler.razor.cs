using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Scheduler;

namespace NTComponents;

/// <summary>
///     Renders a Material 3 inspired scheduler with month, week, and day views.
/// </summary>
/// <typeparam name="TEventType">The event type displayed by the scheduler.</typeparam>
public partial class NTScheduler<TEventType> : TnTPageScriptComponent<NTScheduler<TEventType>> where TEventType : TnTEvent, new() {
    private static readonly NTSchedulerView[] _availableViews = [NTSchedulerView.Month, NTSchedulerView.Week, NTSchedulerView.Day];

    /// <summary>
    ///     Provides an accessible label for the scheduler root.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Scheduler";

    /// <summary>
    ///     The background color of the scheduler surface.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerLow;

    /// <summary>
    ///     The date currently centered by the scheduler.
    /// </summary>
    [Parameter]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    ///     Raised when <see cref="Date" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly> DateChanged { get; set; }

    /// <summary>
    ///     The default duration used for newly created events.
    /// </summary>
    [Parameter]
    public TimeSpan DefaultEventDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    ///     Enables drag and drop for event rescheduling.
    /// </summary>
    [Parameter]
    public bool EnableDragDrop { get; set; } = true;

    /// <summary>
    ///     Enables event creation from empty slots or day cells.
    /// </summary>
    [Parameter]
    public bool EnableEventCreation { get; set; } = true;

    /// <summary>
    ///     Enables event editing when an event is selected.
    /// </summary>
    [Parameter]
    public bool EnableEventEditing { get; set; } = true;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create("nt-scheduler")
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddBackgroundColor(BackgroundColor)
        .AddForegroundColor(TextColor)
        .Build();

    /// <summary>
    ///     The event collection rendered by the scheduler.
    /// </summary>
    [Parameter, EditorRequired]
    public ICollection<TEventType> Events { get; set; } = [];

    /// <summary>
    ///     Raised whenever the scheduler mutates the event collection.
    /// </summary>
    [Parameter]
    public EventCallback<ICollection<TEventType>> EventsChanged { get; set; }

    /// <summary>
    ///     Raised whenever an event is created, updated, moved, or deleted.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerEventChange<TEventType>> EventChanged { get; set; }

    /// <summary>
    ///     Raised when the user selects an existing event.
    /// </summary>
    [Parameter]
    public EventCallback<TEventType> EventClicked { get; set; }

    /// <summary>
    ///     The first day shown in week and month views.
    /// </summary>
    [Parameter]
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Scheduler/NTScheduler.razor.js";

    /// <summary>
    ///     The maximum number of visible event chips rendered in each month cell.
    /// </summary>
    [Parameter]
    public int MaxVisibleMonthEvents { get; set; } = 3;

    /// <summary>
    ///     The default hour used for events created from month cells.
    /// </summary>
    [Parameter]
    public int MonthCellStartHour { get; set; } = 9;

    /// <summary>
    ///     Raised when the user selects a time slot.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerTimeSlot> SlotSelected { get; set; }

    /// <summary>
    ///     The text color used inside the scheduler.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     The number of minutes represented by a time-grid slot.
    /// </summary>
    [Parameter]
    public int TimeSlotIntervalMinutes { get; set; } = 30;

    /// <summary>
    ///     The display title shown in the scheduler toolbar.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Schedule";

    /// <summary>
    ///     The scheduler view currently shown.
    /// </summary>
    [Parameter]
    public NTSchedulerView View { get; set; } = NTSchedulerView.Month;

    /// <summary>
    ///     Raised when <see cref="View" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerView> ViewChanged { get; set; }

    /// <summary>
    ///     The first visible hour in day and week views.
    /// </summary>
    [Parameter]
    public int VisibleHourStart { get; set; } = 6;

    /// <summary>
    ///     The exclusive last visible hour in day and week views.
    /// </summary>
    [Parameter]
    public int VisibleHourEnd { get; set; } = 22;

    private NTSchedulerEditorState<TEventType>? _editorState;
    private string? _editorErrorMessage;

    private bool CanCreate => RendererInfo.IsInteractive && EnableEventCreation;

    private bool CanDrop => RendererInfo.IsInteractive && EnableDragDrop;

    private bool CanDrag => RendererInfo.IsInteractive && EnableDragDrop;

    private bool CanEdit => RendererInfo.IsInteractive && EnableEventEditing;

    private bool CanInteract => RendererInfo.IsInteractive && (EnableEventCreation || EnableEventEditing);

    private string VisibleRangeLabel => View switch {
        NTSchedulerView.Month => Date.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
        NTSchedulerView.Week => $"{GetWeekStart(Date):MMM d} - {GetWeekStart(Date).AddDays(6):MMM d, yyyy}",
        _ => Date.ToString("D", CultureInfo.CurrentCulture)
    };

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        MaxVisibleMonthEvents = Math.Max(1, MaxVisibleMonthEvents);
        MonthCellStartHour = Math.Clamp(MonthCellStartHour, 0, 23);
        TimeSlotIntervalMinutes = Math.Clamp(TimeSlotIntervalMinutes, 15, 60);
        VisibleHourStart = Math.Clamp(VisibleHourStart, 0, 23);
        VisibleHourEnd = Math.Clamp(VisibleHourEnd, VisibleHourStart + 1, 24);
        DefaultEventDuration = DefaultEventDuration <= TimeSpan.Zero ? TimeSpan.FromHours(1) : DefaultEventDuration;
    }

    /// <summary>
    ///     Handles a drop reported by the JavaScript module.
    /// </summary>
    /// <param name="eventId">The dragged event id.</param>
    /// <param name="newStartIso">The new start time encoded as an ISO-8601 string.</param>
    [JSInvokable]
    public async Task HandleJsDropAsync(string eventId, string newStartIso) {
        if (!CanDrag || !int.TryParse(eventId, CultureInfo.InvariantCulture, out var parsedEventId)) {
            return;
        }

        var targetEvent = Events.FirstOrDefault(currentEvent => currentEvent.Id == parsedEventId);
        if (targetEvent is null || !DateTimeOffset.TryParse(newStartIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedStart)) {
            return;
        }

        var previousStart = targetEvent.EventStart;
        var previousEnd = targetEvent.EventEnd;
        var duration = targetEvent.Duration <= TimeSpan.Zero ? DefaultEventDuration : targetEvent.Duration;

        targetEvent.EventStart = parsedStart;
        targetEvent.EventEnd = parsedStart.Add(duration);

        await NotifyEventChangeAsync(targetEvent, NTSchedulerChangeKind.Moved, previousStart, previousEnd);
        await InvokeAsync(StateHasChanged);
    }

    private IReadOnlyList<NTSchedulerMonthCell<TEventType>> BuildMonthCells() {
        var firstOfMonth = new DateOnly(Date.Year, Date.Month, 1);
        var visibleStart = AlignToWeekStart(firstOfMonth);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cells = new List<NTSchedulerMonthCell<TEventType>>(42);

        for (var offset = 0; offset < 42; offset++) {
            var currentDate = visibleStart.AddDays(offset);
            cells.Add(new NTSchedulerMonthCell<TEventType> {
                AccessibleLabel = currentDate.ToString("D", CultureInfo.CurrentCulture),
                Date = currentDate,
                DropStart = CreateLocalDateTimeOffset(currentDate, new TimeOnly(MonthCellStartHour, 0)),
                Events = Events
                    .Where(@event => @event.StartDate <= currentDate && @event.EndDate >= currentDate)
                    .OrderBy(@event => @event.EventStart)
                    .ToArray(),
                HiddenEventCount = 0,
                IsCurrentMonth = currentDate.Month == Date.Month,
                IsToday = currentDate == today
            });
        }

        return cells;
    }

    private IReadOnlyList<NTSchedulerMonthRow<TEventType>> BuildMonthRows() {
        var cells = BuildMonthCells();
        var rows = new List<NTSchedulerMonthRow<TEventType>>(6);

        for (var rowIndex = 0; rowIndex < 6; rowIndex++) {
            var rowCells = cells.Skip(rowIndex * 7).Take(7).ToArray();
            var rowStart = rowCells[0].Date;
            var spans = BuildDateSpans(rowStart, rowCells[^1].Date, Events);
            var visibleSpans = spans.Where(span => span.Lane < MaxVisibleMonthEvents).ToArray();
            var visibleLaneCount = visibleSpans.Length == 0 ? 0 : visibleSpans.Max(span => span.Lane) + 1;
            var updatedCells = rowCells
                .Select(cell => new NTSchedulerMonthCell<TEventType> {
                    AccessibleLabel = cell.AccessibleLabel,
                    Date = cell.Date,
                    DropStart = cell.DropStart,
                    Events = cell.Events,
                    HiddenEventCount = spans.Count(span =>
                        span.Lane >= MaxVisibleMonthEvents &&
                        cell.Date >= GetVisibleSpanStart(rowStart, span) &&
                        cell.Date <= GetVisibleSpanEnd(rowStart, span)),
                    IsCurrentMonth = cell.IsCurrentMonth,
                    IsToday = cell.IsToday
                })
                .ToArray();

            rows.Add(new NTSchedulerMonthRow<TEventType> {
                Cells = updatedCells,
                Spans = visibleSpans,
                VisibleLaneCount = visibleLaneCount
            });
        }

        return rows;
    }

    private IReadOnlyList<NTSchedulerDateSpan<TEventType>> BuildAllDaySpans(int dayCount) {
        var visibleStart = dayCount == 1 ? Date : GetWeekStart(Date);
        return BuildDateSpans(visibleStart, visibleStart.AddDays(dayCount - 1), Events.Where(IsAllDayEvent));
    }

    private IReadOnlyList<NTSchedulerDayColumn<TEventType>> BuildTimeGridColumns(int dayCount) {
        var firstVisibleDate = dayCount == 1 ? Date : GetWeekStart(Date);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var columns = new List<NTSchedulerDayColumn<TEventType>>(dayCount);

        for (var dayIndex = 0; dayIndex < dayCount; dayIndex++) {
            var currentDate = firstVisibleDate.AddDays(dayIndex);
            var eventsForDay = Events
                .Where(@event => @event.StartDate <= currentDate && @event.EndDate >= currentDate)
                .OrderBy(@event => @event.EventStart)
                .ToArray();

            var allDayEvents = eventsForDay
                .Where(IsAllDayEvent)
                .ToArray();

            var timedEvents = BuildTimedLayouts(currentDate, eventsForDay.Where(@event => !IsAllDayEvent(@event)));

            columns.Add(new NTSchedulerDayColumn<TEventType> {
                AccessibleLabel = currentDate.ToString("D", CultureInfo.CurrentCulture),
                AllDayEvents = allDayEvents,
                Date = currentDate,
                IsToday = currentDate == today,
                TimedEvents = timedEvents
            });
        }

        return columns;
    }

    private IReadOnlyList<NTSchedulerTimeSlot> BuildVisibleTimeSlots(NTSchedulerView schedulerView) {
        var firstVisibleDate = schedulerView == NTSchedulerView.Day ? Date : GetWeekStart(Date);
        var totalDays = schedulerView == NTSchedulerView.Day ? 1 : 7;
        var slots = new List<NTSchedulerTimeSlot>();

        for (var dayIndex = 0; dayIndex < totalDays; dayIndex++) {
            var currentDate = firstVisibleDate.AddDays(dayIndex);
            var currentTime = new TimeOnly(VisibleHourStart, 0);
            var lastTime = new TimeOnly(VisibleHourEnd, 0);

            while (currentTime < lastTime) {
                var slotStart = CreateLocalDateTimeOffset(currentDate, currentTime);
                var slotEnd = slotStart.AddMinutes(TimeSlotIntervalMinutes);
                slots.Add(new NTSchedulerTimeSlot {
                    End = slotEnd,
                    Start = slotStart,
                    View = schedulerView
                });

                currentTime = currentTime.AddMinutes(TimeSlotIntervalMinutes);
            }
        }

        return slots;
    }

    private async Task ChangeViewAsync(NTSchedulerView nextView) {
        if (View == nextView) {
            return;
        }

        View = nextView;
        await ViewChanged.InvokeAsync(nextView);
    }

    private void CloseEditor() {
        _editorErrorMessage = null;
        _editorState = null;
    }

    private static DateTimeOffset CreateLocalDateTimeOffset(DateOnly date, TimeOnly time) {
        var localDateTime = date.ToDateTime(time);
        return new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
    }

    private TEventType CreateEventFromEditorState(NTSchedulerEditorState<TEventType> state) {
        return new TEventType {
            BackgroundColor = TnTColor.TertiaryContainer,
            Description = state.Description,
            EventEnd = state.End,
            EventStart = state.Start,
            ForegroundColor = TnTColor.OnTertiaryContainer,
            OnTintColor = TnTColor.OnPrimary,
            TintColor = TnTColor.Primary,
            Title = state.Title.Trim()
        };
    }

    private async Task DeleteEditorEventAsync() {
        if (_editorState?.ExistingEvent is null) {
            return;
        }

        var removedEvent = _editorState.ExistingEvent;
        var previousStart = removedEvent.EventStart;
        var previousEnd = removedEvent.EventEnd;

        Events.Remove(removedEvent);
        CloseEditor();

        await NotifyEventChangeAsync(removedEvent, NTSchedulerChangeKind.Deleted, previousStart, previousEnd);
    }

    private static IReadOnlyList<NTSchedulerTimedEventLayout<TEventType>> BuildTimedLayouts(DateOnly date, IEnumerable<TEventType> sourceEvents) {
        var segments = sourceEvents
            .Select(@event => new LayoutBuilderEntry(@event, date))
            .Where(entry => entry.End > entry.Start)
            .OrderBy(entry => entry.Start)
            .ThenBy(entry => entry.End)
            .ToArray();

        var active = new List<LayoutBuilderEntry>();
        var currentGroup = new List<LayoutBuilderEntry>();
        var finalLayouts = new List<NTSchedulerTimedEventLayout<TEventType>>(segments.Length);
        var currentGroupColumnCount = 0;

        foreach (var entry in segments) {
            active.RemoveAll(activeEntry => activeEntry.End <= entry.Start);
            if (active.Count == 0 && currentGroup.Count > 0) {
                FinalizeGroup(currentGroup, currentGroupColumnCount, finalLayouts);
                currentGroup.Clear();
                currentGroupColumnCount = 0;
            }

            var occupiedColumns = active.Select(activeEntry => activeEntry.ColumnIndex).OrderBy(index => index).ToArray();
            entry.ColumnIndex = 0;
            while (occupiedColumns.Contains(entry.ColumnIndex)) {
                entry.ColumnIndex++;
            }

            active.Add(entry);
            currentGroup.Add(entry);
            currentGroupColumnCount = Math.Max(currentGroupColumnCount, active.Count);
        }

        if (currentGroup.Count > 0) {
            FinalizeGroup(currentGroup, currentGroupColumnCount, finalLayouts);
        }

        return finalLayouts;
    }

    private static void FinalizeGroup(IEnumerable<LayoutBuilderEntry> group, int columnCount, ICollection<NTSchedulerTimedEventLayout<TEventType>> target) {
        foreach (var entry in group) {
            target.Add(new NTSchedulerTimedEventLayout<TEventType> {
                ColumnCount = Math.Max(1, columnCount),
                ColumnIndex = entry.ColumnIndex,
                Event = entry.Event,
                SegmentEnd = entry.End,
                SegmentStart = entry.Start
            });
        }
    }

    private static IReadOnlyList<NTSchedulerDateSpan<TEventType>> BuildDateSpans(DateOnly visibleStart, DateOnly visibleEnd, IEnumerable<TEventType> sourceEvents) {
        if (visibleEnd < visibleStart) {
            return [];
        }

        var laneEndDates = new List<DateOnly>();
        var spans = new List<NTSchedulerDateSpan<TEventType>>();
        var candidates = sourceEvents
            .Where(@event => @event.StartDate <= visibleEnd && @event.EndDate >= visibleStart)
            .OrderBy(@event => @event.StartDate)
            .ThenByDescending(@event => @event.EndDate)
            .ThenBy(@event => @event.Title, StringComparer.CurrentCulture)
            .ToArray();

        foreach (var @event in candidates) {
            var segmentStart = @event.StartDate < visibleStart ? visibleStart : @event.StartDate;
            var segmentEnd = @event.EndDate > visibleEnd ? visibleEnd : @event.EndDate;
            var laneIndex = 0;

            while (laneIndex < laneEndDates.Count && segmentStart <= laneEndDates[laneIndex]) {
                laneIndex++;
            }

            if (laneIndex == laneEndDates.Count) {
                laneEndDates.Add(segmentEnd);
            }
            else {
                laneEndDates[laneIndex] = segmentEnd;
            }

            spans.Add(new NTSchedulerDateSpan<TEventType> {
                ColumnSpan = segmentEnd.DayNumber - segmentStart.DayNumber + 1,
                ContinuesAfter = @event.EndDate > visibleEnd,
                ContinuesBefore = @event.StartDate < visibleStart,
                Event = @event,
                Lane = laneIndex,
                StartColumn = segmentStart.DayNumber - visibleStart.DayNumber
            });
        }

        return spans;
    }

    private double? GetCurrentTimeIndicatorPercent() {
        var now = DateTimeOffset.Now;
        var visibleMinutes = (VisibleHourEnd - VisibleHourStart) * 60.0;
        var startOfView = TimeSpan.FromHours(VisibleHourStart);
        var minutesFromViewStart = now.LocalDateTime.TimeOfDay.Subtract(startOfView).TotalMinutes;

        if (minutesFromViewStart < 0 || minutesFromViewStart > visibleMinutes) {
            return null;
        }

        return minutesFromViewStart / visibleMinutes * 100;
    }

    private string GetViewButtonClass(NTSchedulerView schedulerView) {
        return CssClassBuilder.Create("nt-scheduler__view-button")
            .AddClass("nt-scheduler__view-button--selected", View == schedulerView)
            .Build() ?? string.Empty;
    }

    private static DateOnly GetWeekStart(DateOnly currentDate, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday) {
        var difference = (7 + (currentDate.DayOfWeek - firstDayOfWeek)) % 7;
        return currentDate.AddDays(-difference);
    }

    private async Task GoToNextAsync() {
        var nextDate = View switch {
            NTSchedulerView.Month => Date.AddMonths(1),
            NTSchedulerView.Week => Date.AddDays(7),
            _ => Date.AddDays(1)
        };

        await UpdateDateAsync(nextDate);
    }

    private async Task GoToPreviousAsync() {
        var previousDate = View switch {
            NTSchedulerView.Month => Date.AddMonths(-1),
            NTSchedulerView.Week => Date.AddDays(-7),
            _ => Date.AddDays(-1)
        };

        await UpdateDateAsync(previousDate);
    }

    private async Task GoToTodayAsync() => await UpdateDateAsync(DateOnly.FromDateTime(DateTime.Today));

    private IReadOnlyList<string> GetWeekdayLabels() {
        return Enumerable.Range(0, 7)
            .Select(offset => ((DayOfWeek)(((int)FirstDayOfWeek + offset) % 7)).ToString()[..3])
            .ToArray();
    }

    private async Task HandleEventSelectedAsync(TEventType selectedEvent) {
        await EventClicked.InvokeAsync(selectedEvent);
        if (!CanEdit) {
            return;
        }

        _editorErrorMessage = null;
        _editorState = new NTSchedulerEditorState<TEventType> {
            Description = selectedEvent.Description,
            End = selectedEvent.EventEnd,
            ExistingEvent = selectedEvent,
            IsNewEvent = false,
            Start = selectedEvent.EventStart,
            Title = selectedEvent.Title
        };
    }

    private async Task HandleMonthCellSelectedAsync(DateOnly selectedDate) {
        var start = CreateLocalDateTimeOffset(selectedDate, new TimeOnly(MonthCellStartHour, 0));
        var end = start.Add(DefaultEventDuration);
        await OpenEditorForSlotAsync(new NTSchedulerTimeSlot { End = end, Start = start, View = NTSchedulerView.Month });
    }

    private async Task HandleTimeSlotSelectedAsync(NTSchedulerTimeSlot slot) => await OpenEditorForSlotAsync(slot);

    private static bool IsAllDayEvent(TEventType @event) => @event.StartDate != @event.EndDate;

    private async Task NotifyEventChangeAsync(TEventType targetEvent, NTSchedulerChangeKind changeKind, DateTimeOffset? previousStart = null, DateTimeOffset? previousEnd = null) {
        await EventsChanged.InvokeAsync(Events);
        await EventChanged.InvokeAsync(new NTSchedulerEventChange<TEventType> {
            Event = targetEvent,
            Kind = changeKind,
            PreviousEnd = previousEnd,
            PreviousStart = previousStart
        });
    }

    private async Task OpenEditorForSlotAsync(NTSchedulerTimeSlot slot) {
        await SlotSelected.InvokeAsync(slot);
        if (!CanCreate) {
            return;
        }

        _editorErrorMessage = null;
        _editorState = new NTSchedulerEditorState<TEventType> {
            Description = null,
            End = slot.End,
            ExistingEvent = null,
            IsNewEvent = true,
            Start = slot.Start,
            Title = "New event"
        };
    }

    private async Task SaveEditorAsync() {
        if (_editorState is null) {
            return;
        }

        if (string.IsNullOrWhiteSpace(_editorState.Title)) {
            _editorErrorMessage = "A title is required.";
            return;
        }

        if (_editorState.End <= _editorState.Start) {
            _editorErrorMessage = "End time must be after start time.";
            return;
        }

        if (_editorState.IsNewEvent) {
            var createdEvent = CreateEventFromEditorState(_editorState);
            Events.Add(createdEvent);
            CloseEditor();
            await NotifyEventChangeAsync(createdEvent, NTSchedulerChangeKind.Created);
            return;
        }

        if (_editorState.ExistingEvent is null) {
            return;
        }

        var existingEvent = _editorState.ExistingEvent;
        var previousStart = existingEvent.EventStart;
        var previousEnd = existingEvent.EventEnd;

        existingEvent.Title = _editorState.Title.Trim();
        existingEvent.Description = _editorState.Description;
        existingEvent.EventStart = _editorState.Start;
        existingEvent.EventEnd = _editorState.End;

        CloseEditor();
        await NotifyEventChangeAsync(existingEvent, NTSchedulerChangeKind.Updated, previousStart, previousEnd);
    }

    private async Task UpdateDateAsync(DateOnly nextDate) {
        if (Date == nextDate) {
            return;
        }

        Date = nextDate;
        await DateChanged.InvokeAsync(nextDate);
    }

    private DateOnly AlignToWeekStart(DateOnly value) => GetWeekStart(value, FirstDayOfWeek);

    private static DateOnly GetVisibleSpanStart(DateOnly rowStart, NTSchedulerDateSpan<TEventType> span) => rowStart.AddDays(span.StartColumn);

    private static DateOnly GetVisibleSpanEnd(DateOnly rowStart, NTSchedulerDateSpan<TEventType> span) => rowStart.AddDays(span.StartColumn + span.ColumnSpan - 1);

    private sealed class LayoutBuilderEntry(TEventType @event, DateOnly date) {
        public int ColumnIndex { get; set; }
        public DateTimeOffset End { get; } = @event.EventEnd > CreateLocalDateTimeOffset(date, new TimeOnly(23, 59)) ? CreateLocalDateTimeOffset(date, new TimeOnly(23, 59)) : @event.EventEnd;
        public TEventType Event { get; } = @event;
        public DateTimeOffset Start { get; } = @event.EventStart < CreateLocalDateTimeOffset(date, new TimeOnly(0, 0)) ? CreateLocalDateTimeOffset(date, new TimeOnly(0, 0)) : @event.EventStart;
    }
}
