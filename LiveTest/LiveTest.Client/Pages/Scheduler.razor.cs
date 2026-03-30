using System.Globalization;
using Microsoft.AspNetCore.Components;
using NTComponents.Scheduler;

namespace LiveTest.Client.Pages;
public partial class Scheduler {
    private IReadOnlyList<int> EndHours => Enumerable.Range(VisibleHourStart + 1, 24 - VisibleHourStart).ToArray();
    private IReadOnlyList<int> Hours => Enumerable.Range(0, 24).ToArray();
    private List<string> ActivityLog { get; } = [];
    private TimeSpan DefaultEventDuration { get; } = TimeSpan.FromMinutes(60);
    private bool EnableDragDrop { get; set; } = true;
    private bool EnableEventCreation { get; set; } = true;
    private bool EnableEventEditing { get; set; } = true;
    private DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    private TnTEvent? SelectedEvent { get; set; }
    private NTSchedulerView SelectedView { get; set; } = NTSchedulerView.Month;
    private string SelectedDateInput {
        get => SelectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        set {
            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)) {
                SelectedDate = parsedDate;
            }
        }
    }
    private int TimeSlotIntervalMinutes { get; set; } = 30;
    private int VisibleHourEnd { get; set; } = 19;
    private int VisibleHourStart { get; set; } = 7;
    private DateTime today = DateTime.Today;
    private List<TnTEvent> TasksList = [];

    protected override void OnInitialized() => ResetDemo();

    private void AddLog(string message) {
        ActivityLog.Insert(0, $"{DateTime.Now:t} - {message}");
        if (ActivityLog.Count > 10) {
            ActivityLog.RemoveRange(10, ActivityLog.Count - 10);
        }
    }

    private static string FormatHour(int hour) {
        var dateTime = DateTime.Today.AddHours(hour);
        return dateTime.ToString("h tt", CultureInfo.CurrentCulture);
    }

    private void JumpToToday() {
        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        AddLog("Jumped to today.");
    }

    private void OnDateChanged(DateOnly nextDate) {
        SelectedDate = nextDate;
        AddLog($"Focused date changed to {nextDate:D}.");
    }

    private void OnEventChanged(NTSchedulerEventChange<TnTEvent> change) {
        SelectedEvent = change.Kind == NTSchedulerChangeKind.Deleted ? null : change.Event;
        AddLog($"{change.Kind}: {change.Event.Title} ({change.Event.EventStart:g} - {change.Event.EventEnd:g})");
    }

    private void OnEventClicked(TnTEvent @event) {
        SelectedEvent = @event;
        AddLog($"Opened {@event.Title}.");
    }

    private void OnEventSlotClicked(NTSchedulerTimeSlot slot) {
        AddLog($"Selected open slot starting {slot.Start:g}.");
    }

    private void OnSelectedDateInputChanged(ChangeEventArgs args) {
        SelectedDateInput = args.Value?.ToString() ?? string.Empty;
        AddLog($"Focused date changed to {SelectedDate:D} from the control panel.");
    }

    private void OnViewChanged(NTSchedulerView nextView) {
        SelectedView = nextView;
        AddLog($"View changed to {nextView}.");
    }

    private void ResetDemo() {
        TasksList = [
            new TnTEvent {
                Title = "Quarter kickoff",
                Description = "All-hands review and roadmap alignment.",
                EventStart = today.AddDays(-1).AddHours(9),
                EventEnd = today.AddDays(-1).AddHours(11)
            },
            new TnTEvent {
                Title = "Design review",
                Description = "Review scheduler polish with design and accessibility notes.",
                EventStart = today.AddHours(10),
                EventEnd = today.AddHours(11)
            },
            new TnTEvent {
                Title = "Pairing block",
                Description = "Implement JS-backed drag and drop.",
                EventStart = today.AddHours(11),
                EventEnd = today.AddHours(13)
            },
            new TnTEvent {
                Title = "Lunch with client",
                EventStart = today.AddHours(13),
                EventEnd = today.AddHours(14)
            },
            new TnTEvent {
                Title = "Overlap A",
                EventStart = today.AddDays(1).AddHours(9),
                EventEnd = today.AddDays(1).AddHours(11)
            },
            new TnTEvent {
                Title = "Overlap B",
                EventStart = today.AddDays(1).AddHours(9).AddMinutes(30),
                EventEnd = today.AddDays(1).AddHours(11).AddMinutes(30)
            },
            new TnTEvent {
                Title = "Overlap C",
                EventStart = today.AddDays(1).AddHours(10),
                EventEnd = today.AddDays(1).AddHours(12)
            },
            new TnTEvent {
                Title = "Conference trip",
                Description = "Spans multiple days to show month and all-day rendering.",
                EventStart = today.AddDays(3).AddHours(8),
                EventEnd = today.AddDays(5).AddHours(17)
            },
            new TnTEvent {
                Title = "Release retro",
                EventStart = today.AddDays(6).AddHours(15),
                EventEnd = today.AddDays(6).AddHours(16)
            },
            new TnTEvent {
                Title = "Town hall",
                EventStart = today.AddDays(12).AddHours(14),
                EventEnd = today.AddDays(12).AddHours(15)
            }
        ];

        SelectedDate = DateOnly.FromDateTime(today);
        SelectedEvent = null;
        SelectedView = NTSchedulerView.Month;
        ActivityLog.Clear();
        AddLog("Reset demo events.");
    }
}
