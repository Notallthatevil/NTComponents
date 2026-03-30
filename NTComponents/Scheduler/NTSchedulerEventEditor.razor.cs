using Microsoft.AspNetCore.Components;
using NTComponents.Scheduler;

namespace NTComponents;

/// <summary>
///     Renders the built-in editor surface for <see cref="NTScheduler{TEventType}" />.
/// </summary>
/// <typeparam name="TEventType">The event type displayed by the scheduler.</typeparam>
public partial class NTSchedulerEventEditor<TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Indicates whether the delete action is available.
    /// </summary>
    [Parameter]
    public bool CanDelete { get; set; }

    /// <summary>
    ///     The current validation error message.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Raised when the editor is cancelled.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    ///     Raised when the current event should be deleted.
    /// </summary>
    [Parameter]
    public EventCallback OnDelete { get; set; }

    /// <summary>
    ///     Raised when the current draft should be saved.
    /// </summary>
    [Parameter]
    public EventCallback OnSave { get; set; }

    /// <summary>
    ///     The current editor state.
    /// </summary>
    [Parameter, EditorRequired]
    public NTSchedulerEditorState<TEventType> State { get; set; } = default!;

    private void OnEndChanged(ChangeEventArgs args) => State.EndInputValue = args.Value?.ToString() ?? string.Empty;

    private void OnStartChanged(ChangeEventArgs args) => State.StartInputValue = args.Value?.ToString() ?? string.Empty;
}
