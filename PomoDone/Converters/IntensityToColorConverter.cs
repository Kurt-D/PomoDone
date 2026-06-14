using System.Globalization;

namespace PomoDone.Converters;

// Heatmap intensity (0..4, or -1 placeholder) -> a shade of the brand colour.
public class IntensityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var intensity = value is int i ? i : 0;
        return intensity switch
        {
            < 0 => Colors.Transparent,        // layout placeholder
            0 => Color.FromArgb("#ECEAF6"),   // no focus that day
            1 => Color.FromArgb("#D6CCF5"),
            2 => Color.FromArgb("#B7A6EE"),
            3 => Color.FromArgb("#8C72E0"),
            _ => Color.FromArgb("#512BD4"),   // most intense
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
