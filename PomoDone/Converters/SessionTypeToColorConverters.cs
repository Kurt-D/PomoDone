using System.Globalization;
using PomoDone.Helpers;
using PomoDone.Models;

namespace PomoDone.Converters;

// Session-type selector pills render their selected vs unselected look as a PURE
// FUNCTION of SelectedType, so MAUI never has to "revert" a DataTrigger setter.
// (Trigger revert is unreliable on Android across the Start→Cancel IsEnabled
// transition — it left stale/accumulated accent overrides: one pill stuck, then
// two highlighted at once.) value = SelectedType, parameter = the pill's own
// SessionType; equal ⇒ selected. The property is set explicitly on every
// SelectedType change, so exactly one pill is ever highlighted.
//
// Colours come from ThemeColors (the C#-side {AppThemeBinding}) so they track
// the theme — dark mono-amber / light multicolor — exactly like RingDrawable and
// IntensityToColorConverter. The page re-emits SelectedType on
// RequestedThemeChanged so these re-run on a live theme switch (no stale colour).
public class SessionTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => IsSelected(value, parameter)
            ? ThemeColors.Resolve("VantaAccent")      // selected pill (accent)
            : ThemeColors.Resolve("VantaSurfaceAlt"); // base VantaSelectorPill bg

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    // Selected when the pill's own type (parameter) equals the current selection.
    internal static bool IsSelected(object? value, object? parameter)
        => value is SessionType selected && parameter is SessionType target && selected == target;
}

public class SessionTypeToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => SessionTypeToBackgroundConverter.IsSelected(value, parameter)
            ? Colors.Black                            // selected: black on accent
            : ThemeColors.Resolve("VantaTextMuted");  // base muted text

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
