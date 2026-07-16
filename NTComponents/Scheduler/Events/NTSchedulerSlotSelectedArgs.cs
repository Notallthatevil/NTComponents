namespace NTComponents.Scheduler.Events;

/// <summary>
///     Provides the time range selected by dragging across an empty scheduler day.
/// </summary>
public sealed class NTSchedulerSlotSelectedArgs {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSchedulerSlotSelectedArgs" /> class.
    /// </summary>
    /// <param name="start">The selected range start.</param>
    /// <param name="end">The selected range end.</param>
    public NTSchedulerSlotSelectedArgs(DateTimeOffset start, DateTimeOffset end) {
        Start = start;
        End = end;
    }

    /// <summary>
    ///     Gets the selected range end.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    ///     Gets the selected range start.
    /// </summary>
    public DateTimeOffset Start { get; }
}
