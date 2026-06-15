using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Pages;
using PomoDone.Repositories;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class TimerViewModel : ObservableObject
{
    private readonly SessionRepository _sessions;
    private readonly ISessionAlarmService _alarms;
    private readonly ActiveTaskService _activeTask;
    private readonly TaskItemRepository _tasks;
    private readonly DeckRepository _decks;
    private readonly IDispatcherTimer _tick;

    // Loaded copy of the in-progress session. The SQLite row is the source of
    // truth; remaining time is always recomputed from its StartUtc.
    private Session? _current;
    private bool _completing;

    [ObservableProperty]
    private SessionType _selectedType = SessionType.Focus;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _timeDisplay = "25:00";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _activeTaskTitle = "";

    public bool HasActiveTask => !string.IsNullOrEmpty(ActiveTaskTitle);
    public bool IsIdle => !IsRunning;
    public bool IsFocusSelected => SelectedType == SessionType.Focus;
    public bool IsShortBreakSelected => SelectedType == SessionType.ShortBreak;
    public bool IsLongBreakSelected => SelectedType == SessionType.LongBreak;

    // Quick Review is offered ONLY during a running break — never during a
    // Focus session and never when idle.
    public bool ShowQuickReview => IsRunning && SelectedType != SessionType.Focus;

    public TimerViewModel(
        SessionRepository sessions,
        ISessionAlarmService alarms,
        ActiveTaskService activeTask,
        TaskItemRepository tasks,
        DeckRepository decks,
        IDispatcher dispatcher)
    {
        _sessions = sessions;
        _alarms = alarms;
        _activeTask = activeTask;
        _tasks = tasks;
        _decks = decks;

        // The tick exists ONLY to refresh the display once a second; it never
        // counts anything down. Killing the process loses nothing.
        _tick = dispatcher.CreateTimer();
        _tick.Interval = TimeSpan.FromSeconds(1);
        _tick.Tick += OnTick;
    }

    // Called from TimerPage.OnAppearing. After a process kill or reboot the
    // in-progress row is still in SQLite, so the countdown resumes seamlessly.
    public async Task InitializeAsync()
    {
        if (_current is null)
        {
            var inProgress = await _sessions.GetInProgressAsync();
            if (inProgress is null)
                return;

            _current = inProgress;
            SelectedType = inProgress.Type;
            IsRunning = true;
            _tick.Start();

            // Re-arm the alarm: a reboot clears OS alarms, and re-scheduling
            // after plain process death is harmless (same PendingIntent).
            var endUtc = EndUtcOf(inProgress);
            if (endUtc > DateTime.UtcNow)
                _alarms.ScheduleSessionEnd(inProgress.Type, endUtc);
        }

        await RefreshActiveTaskAsync();
        await RefreshAsync();
    }

    // Reflect whatever task the Tasks tab marked active (the tab's OnAppearing
    // runs each time it's shown, so re-reading here keeps the timer in sync).
    private async Task RefreshActiveTaskAsync()
    {
        var id = _activeTask.ActiveTaskId;
        if (id is null)
        {
            ActiveTaskTitle = "";
            return;
        }

        var task = await _tasks.GetByIdAsync(id.Value);
        if (task is null)
        {
            // The active task was deleted elsewhere — clear the dangling pick.
            _activeTask.Set(null);
            ActiveTaskTitle = "";
        }
        else
        {
            ActiveTaskTitle = task.Title;
        }
    }

    private static int DurationFor(SessionType type) => type switch
    {
        SessionType.ShortBreak => 5,
        SessionType.LongBreak => 15,
        _ => 25,
    };

    [RelayCommand]
    private void SelectType(SessionType type)
    {
        if (IsRunning)
            return;

        SelectedType = type;
    }

    partial void OnSelectedTypeChanged(SessionType value)
    {
        OnPropertyChanged(nameof(IsFocusSelected));
        OnPropertyChanged(nameof(IsShortBreakSelected));
        OnPropertyChanged(nameof(IsLongBreakSelected));
        OnPropertyChanged(nameof(ShowQuickReview));

        if (!IsRunning)
            TimeDisplay = IdleDisplayFor(value);
    }

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(ShowQuickReview));
    }

    partial void OnActiveTaskTitleChanged(string value) => OnPropertyChanged(nameof(HasActiveTask));

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
            return;

        await EnsureNotificationPermissionAsync();

        // Persist before anything else: the row IS the timer. If the process
        // dies a second from now, relaunch resumes from this row.
        var session = new Session
        {
            StartUtc = DateTime.UtcNow,
            DurationMinutes = DurationFor(SelectedType),
            Type = SelectedType,
            Completed = false,
            // Only Focus sessions are tied to a task; breaks carry no TaskId.
            TaskId = SelectedType == SessionType.Focus ? _activeTask.ActiveTaskId : null,
        };
        await _sessions.SaveAsync(session);

        _alarms.ScheduleSessionEnd(session.Type, EndUtcOf(session));

        _current = session;
        StatusMessage = "";
        IsRunning = true;
        _tick.Start();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (_current is null)
            return;

        var abandoned = _current;
        _current = null;
        _tick.Stop();
        _alarms.CancelScheduled();
        await _sessions.DeleteAsync(abandoned);

        IsRunning = false;
        StatusMessage = "Session cancelled.";
        TimeDisplay = IdleDisplayFor(SelectedType);
    }

    // Break-time Quick Review: just navigates to the review screen for a deck.
    // It does NOT touch the in-progress session row or the scheduled alarm — the
    // break keeps running and the end-of-break alarm still fires on time.
    [RelayCommand]
    private async Task QuickReviewAsync()
    {
        var decks = await _decks.GetDecksAsync();
        if (decks.Count == 0)
        {
            await Shell.Current.DisplayAlert("Quick Review", "No decks to review yet.", "OK");
            return;
        }

        Deck target;
        if (decks.Count == 1)
        {
            target = decks[0];
        }
        else
        {
            // Simplest rule that demos cleanly with multiple decks: a one-tap
            // picker. With the seeded deck present, the demo picks it explicitly.
            var names = decks.Select(d => d.Name).ToArray();
            var choice = await Shell.Current.DisplayActionSheet("Review which deck?", "Cancel", null, names);
            var picked = decks.FirstOrDefault(d => d.Name == choice);
            if (picked is null)
                return; // cancelled
            target = picked;
        }

        await Shell.Current.GoToAsync($"{nameof(ReviewPage)}?deckId={target.Id}");
    }

    private async void OnTick(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_current is null)
            return;

        var remaining = EndUtcOf(_current) - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            await CompleteAsync();
            return;
        }

        // Ceiling so a fresh 25-minute session reads 25:00, not 24:59.
        var display = TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
        TimeDisplay = $"{(int)display.TotalMinutes:D2}:{display.Seconds:D2}";
    }

    private async Task CompleteAsync()
    {
        // Guard: a tick can fire while a previous completion write is awaited.
        if (_current is null || _completing)
            return;

        _completing = true;
        _tick.Stop();

        var finished = _current;
        finished.Completed = true;
        await _sessions.SaveAsync(finished);

        _current = null;
        _completing = false;
        IsRunning = false;
        TimeDisplay = IdleDisplayFor(SelectedType);
        StatusMessage = finished.Type == SessionType.Focus
            ? "Focus session complete!"
            : "Break finished.";
    }

    private static string IdleDisplayFor(SessionType type) => $"{DurationFor(type):D2}:00";

    private static DateTime EndUtcOf(Session session) =>
        session.StartUtc.AddMinutes(session.DurationMinutes);

    // POST_NOTIFICATIONS is runtime-grantable on Android 13+; older versions
    // report Granted immediately. A denial never blocks the session — the
    // timer works without the notification.
    private static async Task EnsureNotificationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.PostNotifications>();
    }
}
