using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Repositories;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private const string AvatarPlaceholder = "dotnet_bot.png";

    private readonly UserProfileRepository _profiles;
    private readonly GamificationService _gamification;

    private UserProfile _profile = new();

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string? _avatarPath;

    [ObservableProperty]
    private int _points;

    [ObservableProperty]
    private int _streak;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private int _daysActive;

    [ObservableProperty]
    private int _completedFocusSessions;

    [ObservableProperty]
    private int _reviewsThisWeek;

    [ObservableProperty]
    private string _focusPurity = "100%";

    public ObservableCollection<Badge> Badges { get; } = new();

    // Image.Source binds here so a missing avatar falls back to the placeholder.
    public string AvatarSource => string.IsNullOrEmpty(AvatarPath) ? AvatarPlaceholder : AvatarPath;
    public string StreakDisplay => $"{Streak} {(Streak == 1 ? "day" : "days")}";

    public ProfileViewModel(UserProfileRepository profiles, GamificationService gamification)
    {
        _profiles = profiles;
        _gamification = gamification;
    }

    public async Task LoadAsync()
    {
        _profile = await _profiles.GetAsync() ?? new UserProfile();
        DisplayName = _profile.DisplayName ?? "";
        AvatarPath = _profile.AvatarPath;

        var summary = await _gamification.ComputeAsync();
        Points = summary.Points;
        Streak = summary.Streak;
        Level = summary.Level;
        DaysActive = summary.DaysActive;
        CompletedFocusSessions = summary.CompletedFocusSessions;
        ReviewsThisWeek = summary.ReviewsThisWeek;
        FocusPurity = $"{summary.FocusPurityPercent:0}%";

        Badges.Clear();
        foreach (var badge in summary.Badges)
            Badges.Add(badge);
    }

    // Plain-English explanation of each derived stat, shown via DisplayAlert
    // when the user taps the stat's "ⓘ". Copy mirrors the real derivation in
    // GamificationService — it must stay in sync if that math ever changes.
    // Read-only: opening an alert never touches any stat or navigates.
    private static readonly IReadOnlyDictionary<string, (string Title, string Message)> StatInfo =
        new Dictionary<string, (string, string)>
        {
            ["Level"] = ("Level",
                "Your level rises as you earn points. Thresholds: 0, 50, 120, 220, 350, 520, 740, 1020, 1380, 1840. You start at Level 1 and climb each time your points pass the next mark."),
            ["Points"] = ("Points",
                "Earned two ways: 10 points per completed Focus session, plus 2 points for every flashcard you review on a break. Reviews count whether you got the card right or wrong — what matters is showing up."),
            ["Streak"] = ("Current streak",
                "The number of days in a row, ending today, where you finished at least one Focus session. Miss a day and it resets."),
            ["Purity"] = ("Time in App vs Away",
                "Of your total Focus time, how much you spent inside the app versus in other apps. This is reflection, not a grade — using your notes or a reviewer elsewhere is real studying too. Nothing here is penalized."),
            ["Sessions"] = ("Focus sessions",
                "Total Focus sessions you've completed. Breaks don't count."),
            ["DaysActive"] = ("Days active",
                "The number of separate calendar days you've completed at least one Focus session. Unlike streak, gaps are fine — this just counts every active day."),
            ["Reviews"] = ("Cards reviewed this week",
                "Flashcards you reviewed on breaks since Sunday. This tile is this-week-only — your all-time reviews are what feed your points."),
            ["Badges"] = ("Badges",
                "First Focus: finish 1 Focus session. Getting Started: 10 Focus sessions. Half Century: 50 Focus sessions. On a Roll: 3-day streak. Week Warrior: 7-day streak. Consistent: 14 active days. Reviewer: review 1 flashcard. Study Buddy: review 50 flashcards."),
        };

    [RelayCommand]
    private async Task ShowInfoAsync(string key)
    {
        if (key is not null && StatInfo.TryGetValue(key, out var info))
            await Shell.Current.DisplayAlert(info.Title, info.Message, "OK");
    }

    [RelayCommand]
    private async Task SaveNameAsync()
    {
        _profile.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim();
        await _profiles.SaveAsync(_profile);
    }

    // Pick from the gallery, COPY into app-private storage, and persist that
    // internal path. The gallery content URI is never stored — its read
    // permission expires (CLAUDE.md 3.6).
    [RelayCommand]
    private async Task PickAvatarAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null)
                return;

            var destination = Path.Combine(
                FileSystem.AppDataDirectory, $"avatar_{DateTime.UtcNow.Ticks}.png");

            using (var source = await photo.OpenReadAsync())
            using (var target = File.Create(destination))
            {
                await source.CopyToAsync(target);
            }

            var previous = _profile.AvatarPath;

            _profile.AvatarPath = destination;
            await _profiles.SaveAsync(_profile);
            AvatarPath = destination;

            // Best-effort cleanup of the prior copy.
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                File.Delete(previous);
        }
        catch (Exception)
        {
            // Permission denied, picker cancelled mid-flight, or unsupported —
            // leave the current avatar untouched.
        }
    }

    partial void OnAvatarPathChanged(string? value) => OnPropertyChanged(nameof(AvatarSource));

    partial void OnStreakChanged(int value) => OnPropertyChanged(nameof(StreakDisplay));
}
