using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.ViewModels;

public partial class TimerViewModel : ObservableObject
{
    private readonly SessionRepository _sessions;
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

    public bool IsIdle => !IsRunning;
    public bool IsFocusSelected => SelectedType == SessionType.Focus;
    public bool IsShortBreakSelected => SelectedType == SessionType.ShortBreak;
    public bool IsLongBreakSelected => SelectedType == SessionType.LongBreak;

    public TimerViewModel(SessionRepository sessions, IDispatcher dispatcher)
    {
        _sessions = sessions;

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
        }

        await RefreshAsync();
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

        if (!IsRunning)
            TimeDisplay = IdleDisplayFor(value);
    }

    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(IsIdle));

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
            return;

        // Persist before anything else: the row IS the timer. If the process
        // dies a second from now, relaunch resumes from this row.
        var session = new Session
        {
            StartUtc = DateTime.UtcNow,
            DurationMinutes = DurationFor(SelectedType),
            Type = SelectedType,
            Completed = false,
        };
        await _sessions.SaveAsync(session);

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
        await _sessions.DeleteAsync(abandoned);

        IsRunning = false;
        StatusMessage = "Session cancelled.";
        TimeDisplay = IdleDisplayFor(SelectedType);
    }

    private async void OnTick(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_current is null)
            return;

        var endUtc = _current.StartUtc.AddMinutes(_current.DurationMinutes);
        var remaining = endUtc - DateTime.UtcNow;

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
}
