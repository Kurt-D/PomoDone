using System.Globalization;

namespace PomoDone.Converters;

// Heatmap intensity (0..4, or -1 placeholder) -> an amber ramp on the dark
// Vanta background. Display-only (on-screen heatmap; excluded from PNG export,
// §4.4). Empty days are a faint charcoal tile so the gappy grid stays readable;
// 1..4 climb from dim amber to the full VantaAccent.
public class IntensityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var intensity = value is int i ? i : 0;
        return intensity switch
        {
            < 0 => Colors.Transparent,        // layout placeholder
            0 => Color.FromArgb("#1D1D1D"),   // no focus that day (charcoal tile)
            1 => Color.FromArgb("#5C3D08"),
            2 => Color.FromArgb("#8A5C0A"),
            3 => Color.FromArgb("#C2820B"),
            _ => Color.FromArgb("#F59E0B"),   // most intense (VantaAccent)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
