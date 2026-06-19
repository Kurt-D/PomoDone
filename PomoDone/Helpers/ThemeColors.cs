using Microsoft.Maui;            // AppTheme
using Microsoft.Maui.Controls;   // Application
using Microsoft.Maui.Graphics;   // Color, Colors

namespace PomoDone.Helpers;

// Resolves a theme-token PAIR declared in Resources/Styles/Colors.xaml
// (baseKey + "Dark" / "Light") to the colour for the current app theme.
//
// This is the C#-side equivalent of XAML {AppThemeBinding}: the SkiaSharp /
// IDrawable render sites (charts, timer ring, heatmap converter) cannot consume
// AppThemeBinding, so they call this to pull the SAME tokens — keeping one
// source of truth in Colors.xaml (no second hardcode of the hex).
//
// Theme follows Application.RequestedTheme, which tracks the OS theme until a
// user override (UserAppTheme) is introduced in a later step.
public static class ThemeColors
{
    public static Color Resolve(string baseKey)
    {
        var app = Application.Current;
        var suffix = app?.RequestedTheme == AppTheme.Light ? "Light" : "Dark";

        if (app?.Resources is not null
            && app.Resources.TryGetValue(baseKey + suffix, out var value)
            && value is Color color)
        {
            return color;
        }

        return Colors.Transparent; // token missing — visible-but-harmless fallback
    }
}
