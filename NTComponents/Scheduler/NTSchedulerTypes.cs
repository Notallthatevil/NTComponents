using System.Globalization;

namespace NTComponents.Scheduler;

/// <summary>
///     Defines the available scheduler views.
/// </summary>
public enum NTSchedulerView {

    /// <summary>
    ///     Renders a month grid.
    /// </summary>
    Month,

    /// <summary>
    ///     Renders a seven-day time grid.
    /// </summary>
    Week,

    /// <summary>
    ///     Renders a single-day time grid.
    /// </summary>
    Day
}

/// <summary>
///     Describes the type of event mutation performed by the scheduler.
/// </summary>
public enum NTSchedulerChangeKind {

    /// <summary>
    ///     A new event was created.
    /// </summary>
    Created,

    /// <summary>
    ///     An existing event was updated.
    /// </summary>
    Updated,

    /// <summary>
    ///     An existing event was moved.
    /// </summary>
    Moved,

    /// <summary>
    ///     An existing event was deleted.
    /// </summary>
    Deleted
}

/// <summary>
///     Represents a selected scheduler slot.
/// </summary>
public sealed class NTSchedulerTimeSlot {

    /// <summary>
    ///     Gets or sets the inclusive slot start.
    /// </summary>
    public required DateTimeOffset Start { get; init; }

    /// <summary>
    ///     Gets or sets the exclusive slot end.
    /// </summary>
    public required DateTimeOffset End { get; init; }

    /// <summary>
    ///     Gets or sets the active scheduler view.
    /// </summary>
    public required NTSchedulerView View { get; init; }
}

/// <summary>
///     Represents an event change emitted by the scheduler.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerEventChange<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the affected event.
    /// </summary>
    public required TEventType Event { get; init; }

    /// <summary>
    ///     Gets or sets the kind of change that occurred.
    /// </summary>
    public required NTSchedulerChangeKind Kind { get; init; }

    /// <summary>
    ///     Gets or sets the previous start time when applicable.
    /// </summary>
    public DateTimeOffset? PreviousStart { get; init; }

    /// <summary>
    ///     Gets or sets the previous end time when applicable.
    /// </summary>
    public DateTimeOffset? PreviousEnd { get; init; }
}

/// <summary>
///     Represents a rendered month cell in the scheduler.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerMonthCell<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the accessible label for the cell.
    /// </summary>
    public required string AccessibleLabel { get; init; }

    /// <summary>
    ///     Gets or sets the date represented by the cell.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    ///     Gets or sets the default drop start time for the cell.
    /// </summary>
    public required DateTimeOffset DropStart { get; init; }

    /// <summary>
    ///     Gets or sets the events visible in the cell.
    /// </summary>
    public required IReadOnlyList<TEventType> Events { get; init; }

    /// <summary>
    ///     Gets or sets the number of events hidden from the visible span lanes.
    /// </summary>
    public int HiddenEventCount { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the cell belongs to the active month.
    /// </summary>
    public required bool IsCurrentMonth { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the cell represents today.
    /// </summary>
    public required bool IsToday { get; init; }
}

/// <summary>
///     Represents a rendered row in the scheduler month view.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerMonthRow<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the cells displayed in the week row.
    /// </summary>
    public required IReadOnlyList<NTSchedulerMonthCell<TEventType>> Cells { get; init; }

    /// <summary>
    ///     Gets or sets the visible event spans for the row.
    /// </summary>
    public required IReadOnlyList<NTSchedulerDateSpan<TEventType>> Spans { get; init; }

    /// <summary>
    ///     Gets or sets the number of visible span lanes rendered in the row.
    /// </summary>
    public required int VisibleLaneCount { get; init; }
}

/// <summary>
///     Represents a rendered day column in the scheduler time grid.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerDayColumn<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the accessible label for the column.
    /// </summary>
    public required string AccessibleLabel { get; init; }

    /// <summary>
    ///     Gets or sets the all-day events shown in the column.
    /// </summary>
    public required IReadOnlyList<TEventType> AllDayEvents { get; init; }

    /// <summary>
    ///     Gets or sets the date represented by the column.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the column represents today.
    /// </summary>
    public required bool IsToday { get; init; }

    /// <summary>
    ///     Gets or sets the positioned timed events for the column.
    /// </summary>
    public required IReadOnlyList<NTSchedulerTimedEventLayout<TEventType>> TimedEvents { get; init; }
}

/// <summary>
///     Represents a positioned multi-day span in the scheduler.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerDateSpan<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the number of visible columns covered by the span.
    /// </summary>
    public required int ColumnSpan { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the span continues before the visible range.
    /// </summary>
    public required bool ContinuesBefore { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the span continues after the visible range.
    /// </summary>
    public required bool ContinuesAfter { get; init; }

    /// <summary>
    ///     Gets or sets the underlying event.
    /// </summary>
    public required TEventType Event { get; init; }

    /// <summary>
    ///     Gets or sets the zero-based lane index for the span.
    /// </summary>
    public required int Lane { get; init; }

    /// <summary>
    ///     Gets or sets the zero-based start column for the visible span.
    /// </summary>
    public required int StartColumn { get; init; }
}

/// <summary>
///     Represents the positioned layout for a timed event segment.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerTimedEventLayout<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the total number of columns in the overlap group.
    /// </summary>
    public required int ColumnCount { get; init; }

    /// <summary>
    ///     Gets or sets the zero-based column index for the event.
    /// </summary>
    public required int ColumnIndex { get; init; }

    /// <summary>
    ///     Gets or sets the underlying event.
    /// </summary>
    public required TEventType Event { get; init; }

    /// <summary>
    ///     Gets or sets the visible end of the event segment.
    /// </summary>
    public required DateTimeOffset SegmentEnd { get; init; }

    /// <summary>
    ///     Gets or sets the visible start of the event segment.
    /// </summary>
    public required DateTimeOffset SegmentStart { get; init; }
}

/// <summary>
///     Represents the current draft shown in the built-in scheduler editor.
/// </summary>
/// <typeparam name="TEventType">The scheduler event type.</typeparam>
public sealed class NTSchedulerEditorState<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the event description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the event end.
    /// </summary>
    public required DateTimeOffset End { get; set; }

    /// <summary>
    ///     Gets or sets the source event when editing an existing item.
    /// </summary>
    public required TEventType? ExistingEvent { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether the draft represents a new event.
    /// </summary>
    public required bool IsNewEvent { get; init; }

    /// <summary>
    ///     Gets or sets the event start.
    /// </summary>
    public required DateTimeOffset Start { get; set; }

    /// <summary>
    ///     Gets or sets the event title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the editor-facing end value formatted for a <c>datetime-local</c> input.
    /// </summary>
    public string EndInputValue {
        get => FormatDateTimeLocalValue(End);
        set => End = ParseDateTimeLocalValue(value, End);
    }

    /// <summary>
    ///     Gets or sets the editor-facing start value formatted for a <c>datetime-local</c> input.
    /// </summary>
    public string StartInputValue {
        get => FormatDateTimeLocalValue(Start);
        set => Start = ParseDateTimeLocalValue(value, Start);
    }

    private static string FormatDateTimeLocalValue(DateTimeOffset value) => value.LocalDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDateTimeLocalValue(string? value, DateTimeOffset fallback) {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)) {
            return fallback;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Local));
    }
}
