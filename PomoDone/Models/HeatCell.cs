namespace PomoDone.Models;

// One square in the monthly heatmap. Placeholder cells pad the leading days
// so the first real day lands under the right weekday column.
public class HeatCell
{
    public DateTime? Date { get; init; }
    public double Minutes { get; init; }

    // 0..4 for real days (intensity bucket); -1 marks a layout placeholder.
    public int Intensity { get; init; }

    public bool IsPlaceholder => Intensity < 0;
    public string DayLabel => Date is { } d ? d.Day.ToString() : "";
}
