using System.Globalization;
using PomoDone.Helpers;

namespace PomoDone.Converters;

// Heatmap intensity (0..4, or -1 placeholder) -> the theme's intensity ramp,
// resolved from the Heat0..Heat4 tokens (Dark = amber ramp, Light = green ramp).
// Display-only (on-screen heatmap; excluded from PNG export, §4.4). Empty days
// are the faintest tile so the gappy grid stays readable.
public class IntensityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var intensity = value is int i ? i : 0;
        return intensity switch
        {
            < 0 => Colors.Transparent,            // layout placeholder
            0 => ThemeColors.Resolve("Heat0"),    // no focus that day
            1 => ThemeColors.Resolve("Heat1"),
            2 => ThemeColors.Resolve("Heat2"),
            3 => ThemeColors.Resolve("Heat3"),
            _ => ThemeColors.Resolve("Heat4"),    // most intense
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
