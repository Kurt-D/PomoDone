namespace PomoDone.Models;

// Raw, UI-agnostic buckets computed by StatsService. Both the on-screen
// LiveCharts views and the headless PNG export build their series from this,
// so the exported image always matches what's shown.
public class StatsData
{
    // Weekly Trend: focused minutes for each of the last 7 local days.
    public string[] WeekdayLabels { get; init; } = Array.Empty<string>();
    public double[] WeeklyMinutes { get; init; } = Array.Empty<double>();

    // Peak Hour: focused minutes bucketed by local hour of day (0..23).
    public string[] HourLabels { get; init; } = Array.Empty<string>();
    public double[] HourlyMinutes { get; init; } = Array.Empty<double>();

    // Monthly Heatmap: padded cells for the current local month.
    public IReadOnlyList<HeatCell> Heatmap { get; init; } = Array.Empty<HeatCell>();

    public bool HasData { get; init; }
}
