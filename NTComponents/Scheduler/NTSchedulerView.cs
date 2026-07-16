namespace NTComponents.Scheduler;

/// <summary>
///     Scheduler views supported by <see cref="NTScheduler{TEventType}" />.
/// </summary>
public enum NTSchedulerView {
    /// <summary>
    ///     Shows a six-week month grid.
    /// </summary>
    Month,

    /// <summary>
    ///     Shows a seven-day week with all-day and timed event lanes.
    /// </summary>
    Week,

    /// <summary>
    ///     Shows a single day with all-day and timed event lanes.
    /// </summary>
    Day
}
