using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NTComponents.CodeDocumentation;
using NTComponents.Core;
using NTComponents.Scheduler;
using NTComponents.Scheduler.Events;

namespace NTComponents;

/// <summary>
///     Material 3 scheduler with month, week, and day views.
/// </summary>
/// <typeparam name="TEventType">The event type.</typeparam>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders scheduler markup in static SSR and enhances navigation, callbacks, event dragging, resizing, and time-range selection interactively.",
    CompatibilityDetails = "Static SSR emits the selected calendar view and its events. Date and view navigation, event and slot callbacks, drag-and-drop, resizing, and time-range selection require an interactive render mode or browser enhancement.")]
public partial class NTScheduler<TEventType> where TEventType : TnTEvent {
    /// <summary>
    ///     Gets the isolated JavaScript module path for <see cref="NTScheduler{TEventType}" />.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Scheduler/NTScheduler.razor.js";

    private const int MinutesPerDay = 24 * 60;
    private static readonly TimeOnly EndOfDayTime = new(23, 59, 59, 999);
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private DotNetObjectReference<NTSchedulerJsInteropBridge>? _dotNetObjectRef;
    private IJSObjectReference? _isolatedJsModule;
    private NTSchedulerJsInteropBridge? _jsInteropBridge;

    /// <summary>
    ///     Gets the page script fragment used to enhance static SSR markup when the page loads in the browser.
    /// </summary>
    protected RenderFragment PageScript => builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, nameof(NTPageScript.Src), JsModulePathValue);
        builder.CloseComponent();
    };

    /// <summary>
    ///     The JSRuntime instance used for JavaScript interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-scheduler")
        .AddClass($"nt-scheduler-view-{View.ToString().ToLowerInvariant()}")
        .AddBackgroundColor(BackgroundColor)
        .AddForegroundColor(TextColor)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-scheduler-row-height", $"{HourRowHeight}px")
        .AddVariable("nt-scheduler-event-bg", EventBackgroundColor)
        .AddVariable("nt-scheduler-event-fg", EventTextColor)
        .Build();

    /// <summary>
    ///     Gets or sets whether drag and drop is enabled.
    /// </summary>
    [Parameter]
    public bool AllowDraggingEvents { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether dropped events should be moved by mutating their <see cref="TnTEvent.EventStart" /> and <see cref="TnTEvent.EventEnd" /> values.
    /// </summary>
    [Parameter]
    public bool AutoUpdateEventsOnDrop { get; set; } = true;

    /// <summary>
    ///     Gets or sets the scheduler accessible label.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Scheduler";

    /// <summary>
    ///     Gets or sets the scheduler container color.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerLowest;

    /// <summary>
    ///     Gets or sets the date currently displayed by the scheduler.
    /// </summary>
    [Parameter]
    public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="Date" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<DateTimeOffset> DateChanged { get; set; }

    /// <summary>
    ///     Gets or sets the time-grid interaction snap interval in minutes.
    /// </summary>
    [Parameter]
    public int DragSnapMinutes { get; set; } = 15;

    /// <summary>
    ///     Gets or sets the default event background color when an event does not provide one.
    /// </summary>
    [Parameter]
    public TnTColor EventBackgroundColor { get; set; } = TnTColor.TertiaryContainer;

    /// <summary>
    ///     Gets or sets the callback invoked when an event is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<TEventType> EventClicked { get; set; }

    /// <summary>
    ///     Gets or sets a custom event renderer.
    /// </summary>
    [Parameter]
    public RenderFragment<TEventType>? EventTemplate { get; set; }

    /// <summary>
    ///     Gets or sets the default event text color when an event does not provide one.
    /// </summary>
    [Parameter]
    public TnTColor EventTextColor { get; set; } = TnTColor.OnTertiaryContainer;

    /// <summary>
    ///     Gets or sets the events rendered by the scheduler.
    /// </summary>
    [Parameter, EditorRequired]
    public ICollection<TEventType> Events { get; set; } = [];

    /// <summary>
    ///     Gets or sets the callback invoked after an event is dropped.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerEventDropArgs<TEventType>> EventDropped { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the toolbar should be hidden.
    /// </summary>
    [Parameter]
    public bool HideDateControls { get; set; }

    /// <summary>
    ///     Gets or sets the height of one hour in the week and day views.
    /// </summary>
    [Parameter]
    public int HourRowHeight { get; set; } = 56;

    /// <summary>
    ///     Gets or sets the label for the next navigation button.
    /// </summary>
    [Parameter]
    public string NextButtonAriaLabel { get; set; } = "Next date range";

    /// <summary>
    ///     Gets or sets the label for the previous navigation button.
    /// </summary>
    [Parameter]
    public string PreviousButtonAriaLabel { get; set; } = "Previous date range";

    /// <summary>
    ///     Gets or sets whether event descriptions are shown inside event blocks.
    /// </summary>
    [Parameter]
    public bool ShowDescription { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked when an empty time slot is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<DateTimeOffset> SlotClicked { get; set; }

    /// <summary>
    ///     Gets or sets the callback invoked after dragging across an empty time range in week or day view.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerSlotSelectedArgs> SlotSelected { get; set; }

    /// <summary>
    ///     Gets or sets the first day shown by week-based views.
    /// </summary>
    [Parameter]
    public DayOfWeek StartViewOn { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    ///     Gets or sets the scheduler text color.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     Gets or sets the time zone used to display event dates and create drop target values.
    /// </summary>
    [Parameter]
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>
    ///     Gets or sets the today button label.
    /// </summary>
    [Parameter]
    public string TodayButtonLabel { get; set; } = "Today";

    /// <summary>
    ///     Gets or sets the active scheduler view.
    /// </summary>
    [Parameter]
    public NTSchedulerView View { get; set; } = NTSchedulerView.Month;

    /// <summary>
    ///     Gets or sets the callback invoked when <see cref="View" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<NTSchedulerView> ViewChanged { get; set; }

    private DateOnly DisplayDate => ToDisplayDate(Date);

    private DateOnly PickerDate => View == NTSchedulerView.Week ? StartOfWeek(DisplayDate) : DisplayDate;

    private IReadOnlyList<DateOnly> DayDates => [DisplayDate];

    private IReadOnlyList<DateOnly> MonthDates {
        get {
            var firstOfMonth = new DateOnly(DisplayDate.Year, DisplayDate.Month, 1);
            var start = StartOfWeek(firstOfMonth);
            return [.. Enumerable.Range(0, 42).Select(start.AddDays)];
        }
    }

    private IReadOnlyList<DateOnly> WeekDates {
        get {
            var start = StartOfWeek(DisplayDate);
            return [.. Enumerable.Range(0, 7).Select(start.AddDays)];
        }
    }

    private string ViewTitle => View switch {
        NTSchedulerView.Month => DisplayDate.ToString("MMMM yyyy", _culture),
        NTSchedulerView.Week => $"{WeekDates[0].ToString("MMM d", _culture)} - {WeekDates[^1].ToString(WeekDates[0].Year == WeekDates[^1].Year ? "MMM d, yyyy" : "MMM d, yyyy", _culture)}",
        NTSchedulerView.Day => DisplayDate.ToString("dddd, MMMM d, yyyy", _culture),
        _ => DisplayDate.ToString("D", _culture)
    };

    private RenderFragment DayView => builder => BuildAgendaView(builder, DayDates, true);

    private RenderFragment MonthView => builder => {
        var sequence = 0;
        var today = ToDisplayDate(DateTimeOffset.Now);
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "month-view");
        builder.AddAttribute(sequence++, "role", "grid");
        builder.AddAttribute(sequence++, "aria-label", ViewTitle);

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "month-weekdays");
        foreach (var weekday in GetWeekdayLabels()) {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "weekday-heading");
            builder.AddAttribute(sequence++, "role", "columnheader");
            builder.AddContent(sequence++, weekday);
            builder.CloseElement();
        }
        builder.CloseElement();

        foreach (var week in MonthDates.Chunk(7)) {
            var segments = BuildAllDaySegments(week, true);
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "month-week");
            for (var dayIndex = 0; dayIndex < week.Length; dayIndex++) {
                var date = week[dayIndex];
                builder.OpenElement(sequence++, "button");
                builder.AddAttribute(sequence++, "type", "button");
                builder.AddAttribute(sequence++, "class", GetMonthDayClass(date, today));
                builder.AddAttribute(sequence++, "style", $"grid-column: {dayIndex + 1};");
                builder.AddAttribute(sequence++, "data-nt-scheduler-drop-date", FormatDate(date));
                builder.AddAttribute(sequence++, "data-nt-scheduler-all-day", "true");
                builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => InvokeSlotClickedAsync(date, null)));
                builder.AddEventStopPropagationAttribute(sequence++, "onclick", false);
                builder.OpenElement(sequence++, "span");
                builder.AddAttribute(sequence++, "class", "day-number");
                builder.AddContent(sequence++, date.Day.ToString(_culture));
                builder.CloseElement();
                builder.CloseElement();
            }

            BuildEventSegments(builder, ref sequence, segments, true);

            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private RenderFragment WeekView => builder => BuildAgendaView(builder, WeekDates, false);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        try {
            if (firstRender) {
                _jsInteropBridge ??= new NTSchedulerJsInteropBridge(NotifyEventDroppedAsync, NotifyEventResizedAsync, NotifySlotSelectedAsync);
                _dotNetObjectRef ??= DotNetObjectReference.Create(_jsInteropBridge);
                _isolatedJsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePathValue);
                await (_isolatedJsModule?.InvokeVoidAsync("onLoad", Element, _dotNetObjectRef) ?? ValueTask.CompletedTask);
            }

            await (_isolatedJsModule?.InvokeVoidAsync("onUpdate", Element, _dotNetObjectRef) ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _dotNetObjectRef?.Dispose();
            _dotNetObjectRef = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        if (_isolatedJsModule is not null) {
            try {
                await _isolatedJsModule.InvokeVoidAsync("onDispose", Element);
                await _isolatedJsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }

            _isolatedJsModule = null;
        }

        _dotNetObjectRef?.Dispose();
        _dotNetObjectRef = null;
        await base.DisposeAsyncCore();
    }

    /// <summary>
    ///     Handles a browser-enhanced event drop.
    /// </summary>
    public async Task NotifyEventDroppedAsync(string? eventId, string? date, int? minutes) {
        if (!AllowDraggingEvents || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(date) || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate)) {
            return;
        }

        var @event = Events.FirstOrDefault(candidate => candidate.Id.ToString(CultureInfo.InvariantCulture) == eventId);
        if (@event is null) {
            return;
        }

        var previousStart = @event.EventStart;
        var previousEnd = @event.EventEnd;
        var displayStart = TimeZoneInfo.ConvertTime(previousStart, TimeZone);
        var targetTime = minutes.HasValue ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(Math.Clamp(minutes.Value, 0, MinutesPerDay - DragSnapMinutes))) : TimeOnly.FromDateTime(displayStart.DateTime);
        var newStart = CreateDateTimeOffset(targetDate, targetTime);
        var newEnd = newStart.Add(previousEnd - previousStart);

        if (AutoUpdateEventsOnDrop) {
            @event.EventStart = newStart;
            @event.EventEnd = newEnd;
        }

        await EventDropped.InvokeAsync(new NTSchedulerEventDropArgs<TEventType>(@event, previousStart, previousEnd, newStart, newEnd));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Handles a browser-enhanced event resize.
    /// </summary>
    public async Task NotifyEventResizedAsync(string? eventId, int? startMinutes, int? endMinutes) {
        if (!AllowDraggingEvents || string.IsNullOrWhiteSpace(eventId) || (!startMinutes.HasValue && !endMinutes.HasValue)) {
            return;
        }

        var @event = Events.FirstOrDefault(candidate => candidate.Id.ToString(CultureInfo.InvariantCulture) == eventId);
        if (@event is null) {
            return;
        }

        var previousStart = @event.EventStart;
        var previousEnd = @event.EventEnd;
        var displayStart = TimeZoneInfo.ConvertTime(previousStart, TimeZone);
        var displayEnd = TimeZoneInfo.ConvertTime(previousEnd, TimeZone);
        var eventDate = DateOnly.FromDateTime(displayStart.DateTime);
        var minimumDurationMinutes = Math.Clamp(Math.Max(DragSnapMinutes, 15), 1, MinutesPerDay);
        var currentStartMinute = (displayStart.Hour * 60) + displayStart.Minute;
        var currentEndMinute = DateOnly.FromDateTime(displayEnd.DateTime) > eventDate ? MinutesPerDay : (displayEnd.Hour * 60) + displayEnd.Minute;
        currentEndMinute = Math.Clamp(Math.Max(currentEndMinute, currentStartMinute + minimumDurationMinutes), minimumDurationMinutes, MinutesPerDay);

        var newStartMinute = currentStartMinute;
        var newEndMinute = currentEndMinute;
        if (startMinutes.HasValue) {
            newStartMinute = Math.Clamp(startMinutes.Value, 0, newEndMinute - minimumDurationMinutes);
        }

        if (endMinutes.HasValue) {
            newEndMinute = Math.Clamp(endMinutes.Value, newStartMinute + minimumDurationMinutes, MinutesPerDay);
        }

        var newStart = CreateDateTimeOffset(eventDate, newStartMinute);
        var newEnd = CreateDateTimeOffset(eventDate, newEndMinute);

        if (AutoUpdateEventsOnDrop) {
            @event.EventStart = newStart;
            @event.EventEnd = newEnd;
        }

        await EventDropped.InvokeAsync(new NTSchedulerEventDropArgs<TEventType>(@event, previousStart, previousEnd, newStart, newEnd));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    ///     Handles a browser-enhanced time-range selection.
    /// </summary>
    public async Task NotifySlotSelectedAsync(string? date, int? startMinutes, int? endMinutes) {
        if (!SlotSelected.HasDelegate
            || string.IsNullOrWhiteSpace(date)
            || !startMinutes.HasValue
            || !endMinutes.HasValue
            || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var selectedDate)) {
            return;
        }

        var startMinute = Math.Clamp(startMinutes.Value, 0, MinutesPerDay - 1);
        var endMinute = Math.Clamp(endMinutes.Value, startMinute + 1, MinutesPerDay);
        await SlotSelected.InvokeAsync(new NTSchedulerSlotSelectedArgs(CreateDateTimeOffset(selectedDate, startMinute), CreateDateTimeOffset(selectedDate, endMinute)));
    }

    private void BuildAgendaView(RenderTreeBuilder builder, IReadOnlyList<DateOnly> visibleDates, bool isSingleDay) {
        var sequence = 0;
        var today = ToDisplayDate(DateTimeOffset.Now);
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "agenda-view");

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "agenda-header");
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "time-gutter");
        builder.CloseElement();
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "agenda-days");

        foreach (var date in visibleDates) {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", GetAgendaHeaderClass(date, today));
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "day-name");
            builder.AddContent(sequence++, date.ToString(isSingleDay ? "dddd" : "ddd", _culture));
            builder.CloseElement();
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "day-number");
            builder.AddContent(sequence++, date.Day.ToString(_culture));
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "all-day-row");
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "time-gutter all-day-label");
        builder.AddContent(sequence++, "All day");
        builder.CloseElement();
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "all-day-grid");

        for (var dayIndex = 0; dayIndex < visibleDates.Count; dayIndex++) {
            var date = visibleDates[dayIndex];
            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "type", "button");
            builder.AddAttribute(sequence++, "class", "all-day-drop-zone");
            builder.AddAttribute(sequence++, "style", $"grid-column: {dayIndex + 1};");
            builder.AddAttribute(sequence++, "data-nt-scheduler-drop-date", FormatDate(date));
            builder.AddAttribute(sequence++, "data-nt-scheduler-all-day", "true");
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => InvokeSlotClickedAsync(date, null)));
            builder.CloseElement();
        }

        BuildEventSegments(builder, ref sequence, BuildAllDaySegments(visibleDates, false), false);

        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "time-grid");
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "time-gutter");
        for (var hour = 0; hour < 24; hour++) {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "time-label");
            builder.AddContent(sequence++, new TimeOnly(hour, 0).ToString("h tt", _culture));
            builder.CloseElement();
        }
        builder.CloseElement();

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "timed-days");
        foreach (var date in visibleDates) {
            BuildTimedDay(builder, ref sequence, date);
        }
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private void BuildTimedDay(RenderTreeBuilder builder, ref int sequence, DateOnly date) {
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "timed-day");
        builder.AddAttribute(sequence++, "data-nt-scheduler-drop-date", FormatDate(date));
        builder.AddAttribute(sequence++, "data-nt-scheduler-drop-time", "true");

        for (var minute = 0; minute < MinutesPerDay; minute += 60) {
            var slotMinute = minute;
            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "type", "button");
            builder.AddAttribute(sequence++, "class", "time-slot");
            builder.AddAttribute(sequence++, "data-nt-scheduler-drop-slot", slotMinute.ToString(CultureInfo.InvariantCulture));
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, args => InvokeSlotClickedAsync(date, GetClickedSlotMinute(slotMinute, args.OffsetY))));
            builder.CloseElement();
        }

        BuildEventSegments(builder, ref sequence, BuildTimedSegments(date), false);

        builder.CloseElement();
    }

    private void BuildEventSegments(RenderTreeBuilder builder, ref int sequence, IEnumerable<SchedulerEventSegment> segments, bool monthSegment) {
        builder.OpenRegion(sequence++);
        foreach (var segment in segments) {
            BuildEventSegment(builder, segment, monthSegment);
        }
        builder.CloseRegion();
    }

    private void BuildEventSegment(RenderTreeBuilder builder, SchedulerEventSegment segment, bool monthSegment) {
        var sequence = 0;
        builder.OpenElement(sequence++, "article");
        builder.SetKey($"{segment.Event.Id}:{segment.StartDate:yyyyMMdd}:{segment.EndDate:yyyyMMdd}:{segment.Lane}");
        builder.AddAttribute(sequence++, "class", GetEventClass(segment, monthSegment));
        builder.AddAttribute(sequence++, "style", GetEventStyle(segment, monthSegment));
        builder.AddAttribute(sequence++, "data-nt-scheduler-event-id", segment.Event.Id.ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => EventClicked.InvokeAsync(segment.Event)));
        builder.AddEventStopPropagationAttribute(sequence++, "onclick", true);

        var renderResizeHandles = AllowDraggingEvents && !monthSegment && !IsMultiDay(segment.Event);
        if (renderResizeHandles) {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "resize-handle resize-handle-start");
            builder.AddAttribute(sequence++, "data-nt-scheduler-resize-edge", "start");
            builder.AddAttribute(sequence++, "aria-hidden", "true");
            builder.CloseElement();
        }

        if (EventTemplate is not null) {
            builder.AddContent(sequence++, EventTemplate(segment.Event));
        }
        else {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "event-title");
            builder.AddContent(sequence++, segment.Event.Title);
            builder.CloseElement();

            if (ShowDescription && !string.IsNullOrWhiteSpace(segment.Event.Description)) {
                builder.OpenElement(sequence++, "span");
                builder.AddAttribute(sequence++, "class", "event-description");
                builder.AddContent(sequence++, segment.Event.Description);
                builder.CloseElement();
            }
        }

        if (renderResizeHandles) {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "resize-handle resize-handle-end");
            builder.AddAttribute(sequence++, "data-nt-scheduler-resize-edge", "end");
            builder.AddAttribute(sequence++, "aria-hidden", "true");
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private IReadOnlyList<SchedulerEventSegment> BuildAllDaySegments(IReadOnlyList<DateOnly> dates, bool includeSingleDayEvents) {
        var visibleStart = dates[0];
        var visibleEnd = dates[^1];
        var segments = Events
            .Where(e => includeSingleDayEvents || e.IsAllDay)
            .Select(e => CreateAllDaySegment(e, visibleStart, visibleEnd))
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderBy(s => s.StartDate)
            .ThenByDescending(s => s.ColumnSpan)
            .ThenBy(s => s.Event.EventStart)
            .ToList();
        AssignLanes(segments);
        return segments;
    }

    private IReadOnlyList<SchedulerEventSegment> BuildTimedSegments(DateOnly date) {
        var segments = Events
            .Where(e => !e.IsAllDay && EventOverlaps(e, date, date))
            .Select(e => CreateTimedSegment(e, date))
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderBy(s => s.StartMinute)
            .ThenByDescending(s => s.EndMinute - s.StartMinute)
            .ToList();
        AssignTimedLanes(segments);
        return segments;
    }

    private SchedulerEventSegment? CreateAllDaySegment(TEventType @event, DateOnly visibleStart, DateOnly visibleEnd) {
        var eventStartDate = GetEventStartDate(@event);
        var eventEndDate = GetEventEndDate(@event);
        if (eventEndDate < visibleStart || eventStartDate > visibleEnd) {
            return null;
        }

        var startDate = eventStartDate < visibleStart ? visibleStart : eventStartDate;
        var endDate = eventEndDate > visibleEnd ? visibleEnd : eventEndDate;
        return new SchedulerEventSegment(@event, startDate, endDate) {
            ColumnStart = (startDate.DayNumber - visibleStart.DayNumber) + 1,
            ColumnSpan = (endDate.DayNumber - startDate.DayNumber) + 1,
            ContinuesBefore = eventStartDate < startDate,
            ContinuesAfter = eventEndDate > endDate
        };
    }

    private SchedulerEventSegment? CreateTimedSegment(TEventType @event, DateOnly date) {
        var displayStart = TimeZoneInfo.ConvertTime(@event.EventStart, TimeZone);
        var displayEnd = TimeZoneInfo.ConvertTime(@event.EventEnd, TimeZone);
        var startMinute = GetEventStartDate(@event) < date ? 0 : (displayStart.Hour * 60) + displayStart.Minute;
        var endMinute = GetEventEndDate(@event) > date ? MinutesPerDay : (displayEnd.Hour * 60) + displayEnd.Minute;
        endMinute = Math.Max(startMinute + DragSnapMinutes, endMinute);

        return EventOverlaps(@event, date, date)
            ? new SchedulerEventSegment(@event, date, date) {
                StartMinute = Math.Clamp(startMinute, 0, MinutesPerDay - 1),
                EndMinute = Math.Clamp(endMinute, DragSnapMinutes, MinutesPerDay),
                ColumnStart = 1,
                ColumnSpan = 1
            }
            : null;
    }

    private static void AssignLanes(List<SchedulerEventSegment> segments) {
        var laneEnds = new List<DateOnly>();
        foreach (var segment in segments) {
            var lane = laneEnds.FindIndex(end => segment.StartDate > end);
            if (lane < 0) {
                lane = laneEnds.Count;
                laneEnds.Add(segment.EndDate);
            }
            else {
                laneEnds[lane] = segment.EndDate;
            }

            segment.Lane = lane;
        }
    }

    private static void AssignTimedLanes(List<SchedulerEventSegment> segments) {
        var lanes = new List<List<SchedulerEventSegment>>();
        foreach (var segment in segments) {
            var lane = lanes.FindIndex(existingSegments => existingSegments.All(existing => !TimedEventsOverlap(existing, segment)));
            if (lane < 0) {
                lane = lanes.Count;
                lanes.Add([]);
            }

            segment.Lane = lane;
            lanes[lane].Add(segment);
        }

        foreach (var segment in segments) {
            segment.LaneCount = GetTimedLaneCount(segment, segments);
        }
    }

    private static int GetTimedLaneCount(SchedulerEventSegment segment, List<SchedulerEventSegment> segments) {
        var connectedSegments = new HashSet<SchedulerEventSegment> { segment };
        var pendingSegments = new Queue<SchedulerEventSegment>();
        pendingSegments.Enqueue(segment);

        while (pendingSegments.Count > 0) {
            var current = pendingSegments.Dequeue();
            foreach (var candidate in segments) {
                if (connectedSegments.Contains(candidate) || !TimedEventsOverlap(current, candidate)) {
                    continue;
                }

                connectedSegments.Add(candidate);
                pendingSegments.Enqueue(candidate);
            }
        }

        return connectedSegments.Max(s => s.Lane) + 1;
    }

    private static bool TimedEventsOverlap(SchedulerEventSegment first, SchedulerEventSegment second) => first.StartMinute < second.EndMinute && second.StartMinute < first.EndMinute;

    private DateTimeOffset CreateDateTimeOffset(DateOnly date, TimeOnly time) {
        var localDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, TimeZone.GetUtcOffset(localDateTime));
    }

    private DateTimeOffset CreateDateTimeOffset(DateOnly date, int minute) {
        var clampedMinute = Math.Clamp(minute, 0, MinutesPerDay);
        return clampedMinute == MinutesPerDay
            ? CreateDateTimeOffset(date.AddDays(1), TimeOnly.MinValue)
            : CreateDateTimeOffset(date, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(clampedMinute)));
    }

    private bool EventOverlaps(TEventType @event, DateOnly startDate, DateOnly endDate) => GetEventStartDate(@event) <= endDate && GetEventEndDate(@event) >= startDate;

    private string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private IEnumerable<string> GetWeekdayLabels() {
        var start = StartOfWeek(DisplayDate);
        for (var i = 0; i < 7; i++) {
            yield return start.AddDays(i).ToString("ddd", _culture);
        }
    }

    private string GetAgendaHeaderClass(DateOnly date, DateOnly today) => CssClassBuilder.Create("day-heading")
        .AddClass("today", date == today)
        .Build();

    private string GetEventClass(SchedulerEventSegment segment, bool monthSegment) => CssClassBuilder.Create("event")
        .AddClass("event-month", monthSegment)
        .AddClass("event-timed", !monthSegment && !segment.Event.IsAllDay)
        .AddClass("event-draggable", AllowDraggingEvents)
        .AddClass("event-continues-before", segment.ContinuesBefore)
        .AddClass("event-continues-after", segment.ContinuesAfter)
        .AddRipple(EventClicked.HasDelegate)
        .Build();

    private string? GetEventStyle(SchedulerEventSegment segment, bool monthSegment) {
        var backgroundColor = string.IsNullOrWhiteSpace(segment.Event.BackgroundColorCss)
            ? (segment.Event.BackgroundColor == default ? EventBackgroundColor : segment.Event.BackgroundColor).ToCssTnTColorVariable()
            : segment.Event.BackgroundColorCss.Trim();
        var foregroundColor = string.IsNullOrWhiteSpace(segment.Event.ForegroundColorCss)
            ? (segment.Event.ForegroundColor == default ? EventTextColor : segment.Event.ForegroundColor).ToCssTnTColorVariable()
            : segment.Event.ForegroundColorCss.Trim();
        var builder = CssStyleBuilder.Create()
            .AddVariable("event-bg", backgroundColor)
            .AddVariable("event-fg", foregroundColor)
            .AddVariable("event-column-start", segment.ColumnStart.ToString(CultureInfo.InvariantCulture))
            .AddVariable("event-column-span", segment.ColumnSpan.ToString(CultureInfo.InvariantCulture))
            .AddVariable("event-lane", (segment.Lane + (monthSegment ? 2 : 1)).ToString(CultureInfo.InvariantCulture));

        if (!monthSegment && !segment.Event.IsAllDay) {
            builder.AddVariable("event-start-minute", segment.StartMinute.ToString(CultureInfo.InvariantCulture))
                .AddVariable("event-end-minute", segment.EndMinute.ToString(CultureInfo.InvariantCulture))
                .AddVariable("event-lane-count", segment.LaneCount.ToString(CultureInfo.InvariantCulture));
        }

        return builder.Build();
    }

    private string GetMonthDayClass(DateOnly date, DateOnly today) => CssClassBuilder.Create("month-day")
        .AddClass("outside-month", date.Month != DisplayDate.Month)
        .AddClass("today", date == today)
        .Build();

    private DateOnly GetEventEndDate(TEventType @event) {
        var displayEnd = TimeZoneInfo.ConvertTime(@event.EventEnd, TimeZone);
        var displayStart = TimeZoneInfo.ConvertTime(@event.EventStart, TimeZone);
        var endDate = DateOnly.FromDateTime(displayEnd.DateTime);
        return displayEnd.TimeOfDay == TimeSpan.Zero && displayEnd > displayStart ? endDate.AddDays(-1) : endDate;
    }

    private DateOnly GetEventStartDate(TEventType @event) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(@event.EventStart, TimeZone).DateTime);

    private int GetClickedSlotMinute(int hourStartMinute, double offsetY) {
        var snapMinutes = Math.Clamp(DragSnapMinutes, 1, MinutesPerDay);
        var minuteWithinHour = Math.Clamp(offsetY, 0, Math.Max(HourRowHeight, 1)) / Math.Max(HourRowHeight, 1) * 60;
        var clickedMinute = hourStartMinute + minuteWithinHour;
        var snappedMinute = (int)Math.Round(clickedMinute / snapMinutes, MidpointRounding.AwayFromZero) * snapMinutes;
        return Math.Clamp(snappedMinute, 0, MinutesPerDay - snapMinutes);
    }

    private Task GoToTodayAsync() => SetDateAsync(DateTimeOffset.Now);

    private bool IsMultiDay(TEventType @event) => GetEventStartDate(@event) != GetEventEndDate(@event);

    private Task InvokeSlotClickedAsync(DateOnly date, int? minute) {
        if (!SlotClicked.HasDelegate) {
            return Task.CompletedTask;
        }

        var time = minute.HasValue ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minute.Value)) : TimeOnly.MinValue;
        return SlotClicked.InvokeAsync(CreateDateTimeOffset(date, time));
    }

    private Task NextPageAsync() => SetDateAsync(View switch {
        NTSchedulerView.Month => Date.AddMonths(1),
        NTSchedulerView.Week => Date.AddDays(7),
        NTSchedulerView.Day => Date.AddDays(1),
        _ => Date
    });

    private Task PreviousPageAsync() => SetDateAsync(View switch {
        NTSchedulerView.Month => Date.AddMonths(-1),
        NTSchedulerView.Week => Date.AddDays(-7),
        NTSchedulerView.Day => Date.AddDays(-1),
        _ => Date
    });

    private Task SetDisplayDateAsync(DateOnly date) => SetDateAsync(CreateDateTimeOffset(date, TimeOnly.MinValue));

    private async Task SetDateAsync(DateTimeOffset date) {
        Date = date;
        await DateChanged.InvokeAsync(date);
    }

    private async Task SetViewAsync(NTSchedulerView view) {
        View = view;
        await ViewChanged.InvokeAsync(view);
    }

    private DateOnly StartOfWeek(DateOnly date) {
        var diff = (7 + (date.DayOfWeek - StartViewOn)) % 7;
        return date.AddDays(-diff);
    }

    private DateOnly ToDisplayDate(DateTimeOffset date) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(date, TimeZone).DateTime);

    private sealed class SchedulerEventSegment {
        public SchedulerEventSegment(TEventType @event, DateOnly startDate, DateOnly endDate) {
            Event = @event;
            StartDate = startDate;
            EndDate = endDate;
        }

        public int ColumnSpan { get; init; } = 1;
        public int ColumnStart { get; init; } = 1;
        public bool ContinuesAfter { get; init; }
        public bool ContinuesBefore { get; init; }
        public DateOnly EndDate { get; }
        public int EndMinute { get; init; }
        public TEventType Event { get; }
        public int Lane { get; set; }
        public int LaneCount { get; set; } = 1;
        public DateOnly StartDate { get; }
        public int StartMinute { get; init; }
    }
}
