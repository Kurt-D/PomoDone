using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PomoDone.Models;
using SkiaSharp;

namespace PomoDone.Services;

public record ChartConfig(ISeries[] Series, ICartesianAxis[] XAxes, ICartesianAxis[] YAxes);

// Builds the LiveCharts2 series + axes from StatsData. Called once for the
// on-screen chart and again (fresh instances) for the headless PNG export, so
// the exported image is the same chart definition — not a screenshot.
public static class StatsChartFactory
{
    private static readonly SKColor Primary = new(0x51, 0x2B, 0xD4);

    public static ChartConfig Weekly(StatsData data)
    {
        var series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = data.WeeklyMinutes,
                Name = "Focus minutes",
                Fill = null,
                GeometrySize = 8,
                Stroke = new SolidColorPaint(Primary, 3),
                GeometryStroke = new SolidColorPaint(Primary, 3),
                GeometryFill = new SolidColorPaint(SKColors.White),
            },
        };

        var xAxes = new ICartesianAxis[] { new Axis { Labels = data.WeekdayLabels } };
        var yAxes = new ICartesianAxis[] { new Axis { Name = "Minutes", MinLimit = 0 } };
        return new ChartConfig(series, xAxes, yAxes);
    }

    public static ChartConfig PeakHour(StatsData data)
    {
        var series = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = data.HourlyMinutes,
                Name = "Minutes",
                Fill = new SolidColorPaint(Primary),
            },
        };

        // Horizontal bars: hour categories on Y, minute values on X.
        var xAxes = new ICartesianAxis[] { new Axis { Name = "Minutes", MinLimit = 0 } };
        var yAxes = new ICartesianAxis[] { new Axis { Labels = data.HourLabels } };
        return new ChartConfig(series, xAxes, yAxes);
    }
}
