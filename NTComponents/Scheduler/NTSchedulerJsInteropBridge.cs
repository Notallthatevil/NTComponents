using Microsoft.JSInterop;

namespace NTComponents.Scheduler;

/// <summary>
///     Non-generic JavaScript callback bridge for <see cref="NTScheduler{TEventType}" />.
/// </summary>
public sealed class NTSchedulerJsInteropBridge {
    private readonly Func<string?, string?, int?, Task> _eventDropped;
    private readonly Func<string?, int?, int?, Task> _eventResized;
    private readonly Func<string?, int?, int?, Task> _slotSelected;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSchedulerJsInteropBridge" /> class.
    /// </summary>
    /// <param name="eventDropped">The event drop callback.</param>
    /// <param name="eventResized">The event resize callback.</param>
    /// <param name="slotSelected">The time-range selection callback.</param>
    public NTSchedulerJsInteropBridge(Func<string?, string?, int?, Task> eventDropped, Func<string?, int?, int?, Task> eventResized, Func<string?, int?, int?, Task> slotSelected) {
        _eventDropped = eventDropped;
        _eventResized = eventResized;
        _slotSelected = slotSelected;
    }

    /// <summary>
    ///     Handles a browser-enhanced event drop.
    /// </summary>
    [JSInvokable]
    public Task NotifyEventDroppedAsync(string? eventId, string? date, int? minutes) => _eventDropped(eventId, date, minutes);

    /// <summary>
    ///     Handles a browser-enhanced event resize.
    /// </summary>
    [JSInvokable]
    public Task NotifyEventResizedAsync(string? eventId, int? startMinutes, int? endMinutes) => _eventResized(eventId, startMinutes, endMinutes);

    /// <summary>
    ///     Handles a browser-enhanced time-range selection.
    /// </summary>
    [JSInvokable]
    public Task NotifySlotSelectedAsync(string? date, int? startMinutes, int? endMinutes) => _slotSelected(date, startMinutes, endMinutes);
}
