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
        FocusPurity = $"{summary.FocusPurityPercent:0}%";

        Badges.Clear();
        foreach (var badge in summary.Badges)
            Badges.Add(badge);
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
