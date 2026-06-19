using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Pages;
using PomoDone.Repositories;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class TimerViewModel : ObservableObject
{
    // After this many completed Focus sessions today, suggest a Long Break. A
    // suggestion ONLY — it never changes the selected type or blocks Start (§3.1).
    private const int LongBreakSuggestionThreshold = 3;

    private readonly SessionRepository _sessions;
    private readonly ISessionAlarmService _alarms;
    private readonly ActiveTaskService _activeTask;
    private readonly TaskItemRepository _tasks;
    private readonly DeckRepository _decks;
    private readonly GamificationService _gamification;
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

    // Fraction of the session still remaining (1 = just started, 0 = done).
    // Display-only: the countdown ring binds to it. Defaults full so the idle
    // ring reads as a ready, complete circle.
    [ObservableProperty]
    private double _progress = 1;

    // Derived gamification values (CLAUDE.md §3.5), reused from the same
    // GamificationService the Profile page uses — NOT new/stored numbers. Shown
    // in the Timer's streak/XP pills.
    [ObservableProperty]
    private int _streak;

    [ObservableProperty]
    private int _points;

    // Today's completed Focus session count (derived, §3.5) — drives the
    // adaptive Long Break suggestion below. Display-only.
    [ObservableProperty]
    private int _todayFocusCount;

    public bool HasActiveTask => !string.IsNullOrEmpty(ActiveTaskTitle);
    public bool IsIdle => !IsRunning;

    // Adaptive break hint: shown only while idle, once enough Focus sessions are
    // done today and a Long Break isn't already selected. A nudge, not an action —
    // it never auto-changes SelectedType or blocks Start.
    public bool ShowLongBreakSuggestion =>
        IsIdle && TodayFocusCount >= LongBreakSuggestionThreshold && SelectedType != SessionType.LongBreak;

    public string LongBreakSuggestion =>
        $"You've completed {TodayFocusCount} focus sessions today — consider a Long Break to recharge.";

    // Card shows current task when set; placeholder when not. Hint label
    // reflects state so the card always looks intentional and tappable.
    public string ActiveTaskDisplay => HasActiveTask ? ActiveTaskTitle : "Tap to set active task…";
    public string ActiveTaskHint => HasActiveTask ? "Current task" : "No active task";

    // Pill / ring label text.
    public string StreakDisplay => $"{Streak} day streak";
    public string PointsDisplay => $"{Points:N0} XP";
    public string SessionTypeLabel => SelectedType switch
    {
        SessionType.ShortBreak => "SHORT BREAK",
        SessionType.LongBreak => "LONG BREAK",
        _ => "FOCUS",
    };

    // Quick Review is offered ONLY during a running break — never during a
    // Focus session and never when idle.
    public bool ShowQuickReview => IsRunning && SelectedType != SessionType.Focus;

    public TimerViewModel(
        SessionRepository sessions,
        ISessionAlarmService alarms,
        ActiveTaskService activeTask,
        TaskItemRepository tasks,
        DeckRepository decks,
        GamificationService gamification,
        IDispatcher dispatcher)
    {
        _sessions = sessions;
        _alarms = alarms;
        _activeTask = activeTask;
        _tasks = tasks;
        _decks = decks;
        _gamification = gamification;

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
        // Resume setup runs only once, when there's a persisted in-progress row
        // and we haven't already loaded it. The refreshes below run on EVERY
        // appearing (including the idle case) so the active-task title reflects
        // a pick made on the Tasks tab without needing a leave/return.
        if (_current is null)
        {
            var inProgress = await _sessions.GetInProgressAsync();
            if (inProgress is not null)
            {
                _current = inProgress;
                SelectedType = inProgress.Type;
                IsRunning = true;
                _tick.Start();

                // Resume gap: the in-progress row carries its TaskId, but after
                // a process kill the ActiveTaskService was reloaded from
                // Preferences independently. Re-pin from the row so a resumed
                // Focus session re-shows its task title (RefreshActiveTaskAsync
                // reads the service).
                if (inProgress.TaskId is int taskId)
                    _activeTask.Set(taskId);

                // Re-arm the alarm: a reboot clears OS alarms, and re-scheduling
                // after plain process death is harmless (same PendingIntent).
                var endUtc = EndUtcOf(inProgress);
                if (endUtc > DateTime.UtcNow)
                    _alarms.ScheduleSessionEnd(inProgress.Type, endUtc);
            }
        }

        await RefreshActiveTaskAsync();
        await RefreshAsync();
        await RefreshGamificationAsync();
        await RefreshTodayFocusCountAsync();
    }

    // Adaptive break hint (Part 2): a derived, display-only read. Counts today's
    // completed Focus sessions (local day, §3.4) so the VM can suggest a Long
    // Break past the threshold. Reuses the existing SessionRepository (singleton
    // connection, §3.4); computes nothing stored and never touches timer logic.
    private async Task RefreshTodayFocusCountAsync()
    {
        var all = await _sessions.GetAllAsync();
        var today = DateTime.Now.Date;
        TodayFocusCount = all.Count(s =>
            s.Type == SessionType.Focus
            && s.Completed
            && StreakMath.ToLocalDate(s.StartUtc) == today);
    }

    // Pull the derived streak/points from the shared GamificationService (the
    // same source the Profile page uses). Read-only — computes nothing new and
    // stores nothing (§3.5). Refreshed on appearing and after a completion so
    // the pills stay current.
    private async Task RefreshGamificationAsync()
    {
        var summary = await _gamification.ComputeAsync();
        Streak = summary.Streak;
        Points = summary.Points;
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
        // The pill BackgroundColor/TextColor bindings have SelectedType as their
        // source, so the generated setter's own PropertyChanged already re-runs
        // the converters — no per-pill bool notifications needed.
        OnPropertyChanged(nameof(ShowQuickReview));
        OnPropertyChanged(nameof(SessionTypeLabel));
        OnPropertyChanged(nameof(ShowLongBreakSuggestion));

        if (!IsRunning)
        {
            TimeDisplay = IdleDisplayFor(value);
            Progress = 1; // idle ring reads full for the newly selected type
        }
    }

    // Force the session-type pill converters (BackgroundColor/TextColor bound to
    // SelectedType, resolved in C# via ThemeColors) to re-run after a LIVE theme
    // switch. The value is unchanged, so the bindings must be nudged — same idea
    // as the heatmap/ring/chart re-theme fix. Render-only: re-raises the
    // notification, never runs SelectedType's change logic (§3.1 untouched).
    public void RefreshSelectedTypeColors() => OnPropertyChanged(nameof(SelectedType));

    partial void OnStreakChanged(int value) => OnPropertyChanged(nameof(StreakDisplay));

    partial void OnPointsChanged(int value) => OnPropertyChanged(nameof(PointsDisplay));

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(ShowQuickReview));
        OnPropertyChanged(nameof(ShowLongBreakSuggestion));
    }

    partial void OnTodayFocusCountChanged(int value)
    {
        OnPropertyChanged(nameof(ShowLongBreakSuggestion));
        OnPropertyChanged(nameof(LongBreakSuggestion));
    }

    partial void OnActiveTaskTitleChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveTask));
        OnPropertyChanged(nameof(ActiveTaskDisplay));
        OnPropertyChanged(nameof(ActiveTaskHint));
    }

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
        await RefreshActiveTaskAsync();
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
        Progress = 1; // reset ring to full (idle)
    }

    // Task picker: opens a modal action sheet listing not-done tasks. Writes
    // through the ONE ActiveTaskService write path (_activeTask.Set) — the same
    // call site the old Tasks ⋮ "Set Active" used. No duplicate mechanism.
    [RelayCommand]
    private async Task PickActiveTaskAsync()
    {
        var allTasks = await _tasks.GetAllAsync();
        var notDone = allTasks
            .Where(t => !t.IsDone)
            .OrderByDescending(t => t.IsFavorite)
            .ThenByDescending(t => t.CreatedUtc)
            .ToList();

        if (notDone.Count == 0)
        {
            await Shell.Current.DisplayAlert("No tasks", "Add a task on the Tasks tab first.", "OK");
            return;
        }

        var names = notDone.Select(t => t.Title).ToArray();
        var choice = await Shell.Current.DisplayActionSheet(
            "Set active task", "Cancel", "Clear active", names);

        if (choice is null || choice == "Cancel")
            return;

        if (choice == "Clear active")
            _activeTask.Set(null);
        else
        {
            var picked = notDone.FirstOrDefault(t => t.Title == choice);
            if (picked is not null)
                _activeTask.Set(picked.Id);
        }

        await RefreshActiveTaskAsync();
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

        // Display-only ring fill: fraction of the session still remaining,
        // derived from the same wall-clock values above. No timing logic added.
        var total = _current.DurationMinutes * 60.0;
        Progress = total > 0 ? Math.Clamp(remaining.TotalSeconds / total, 0, 1) : 0;
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
        Progress = 1; // reset ring to full (idle)
        StatusMessage = finished.Type == SessionType.Focus
            ? "Focus session complete!"
            : "Break finished.";

        // A completed Focus session changes derived points/streak — refresh the
        // pills so they reflect it immediately.
        await RefreshGamificationAsync();

        // Today's completed-Focus count just changed too; re-read so the adaptive
        // Long Break suggestion can appear once the threshold is crossed.
        await RefreshTodayFocusCountAsync();
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
