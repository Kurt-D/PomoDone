using Microsoft.Maui;            // AppTheme
using Microsoft.Maui.Controls;   // Application
using Microsoft.Maui.Storage;    // Preferences

namespace PomoDone.Helpers;

// Owns the in-app dark/light choice: applies it to Application.UserAppTheme and
// persists it in Preferences (NOT SQLite — theme is display-only and stays out
// of the frozen §5 schema). Default is Dark (the app's identity) when unset.
//
// Setting UserAppTheme pins the app theme (it overrides the OS setting) and
// raises Application.RequestedThemeChanged, which the render-site pages listen
// to so C#-resolved colours re-resolve immediately.
public static class ThemeManager
{
    public const string PreferenceKey = "AppTheme";

    // Apply the saved choice at startup, before the first page renders. Unset →
    // Dark.
    public static void ApplySavedTheme()
    {
        var saved = Preferences.Get(PreferenceKey, nameof(AppTheme.Dark));
        var theme = string.Equals(saved, nameof(AppTheme.Light), StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Light
            : AppTheme.Dark;
        Apply(theme, persist: false);
    }

    // True when the app is currently in (or defaulting to) dark.
    public static bool IsDark =>
        (Application.Current?.UserAppTheme ?? AppTheme.Dark) != AppTheme.Light;

    // Set + persist the user's choice (used by the ProfilePage toggle).
    public static void Set(bool dark) => Apply(dark ? AppTheme.Dark : AppTheme.Light, persist: true);

    private static void Apply(AppTheme theme, bool persist)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = theme;
        if (persist)
            Preferences.Set(PreferenceKey, theme.ToString());
    }
}
