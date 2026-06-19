using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PomoDone.Helpers;
using PomoDone.Models;
using SkiaSharp;

namespace PomoDone.Services;

public record ChartConfig(ISeries[] Series, ICartesianAxis[] XAxes, ICartesianAxis[] YAxes);

// Builds the LiveCharts2 series + axes from StatsData.
//
// TWO STYLINGS, ONE DATA SET (same buckets/labels/limits in both — only paint
// differs, so the analytics definitions in §4.4 are unchanged):
//   • Weekly / PeakHour      → EXPORT styling (brand purple on the white canvas
//     the headless ChartImageRenderer sets). Used by the PNG export path. DO
//     NOT change these or the exported image drifts (§4.4).
//   • WeeklyDark / PeakHourDark → ON-SCREEN styling (amber series + light axis
//     text) for legibility on the dark Vanta background. Used by the page only.
public static class StatsChartFactory
{
    // EXPORT-only brand colour — FIXED, never theme-resolved (§4.4). Used only
    // by Weekly / PeakHour below, which the PNG export path calls.
    private static readonly SKColor Primary = new(0x51, 0x2B, 0xD4);

    // On-screen paints resolve from the Colors.xaml theme tokens at build time
    // (Dark = the prior mono-amber look; Light = the multicolor hues). Single
    // source of truth; used ONLY by the on-screen *Dark builders.
    private static SKColor Sk(string baseKey)
    {
        var c = ThemeColors.Resolve(baseKey);
        return new SKColor(
            (byte)(c.Red * 255f),
            (byte)(c.Green * 255f),
            (byte)(c.Blue * 255f),
            (byte)(c.Alpha * 255f));
    }

    // ---- EXPORT styling (unchanged) -------------------------------------

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

    // ---- ON-SCREEN styling (dark Vanta theme) ---------------------------
    // Same Values / Labels / MinLimit as the export methods above — only the
    // paints change. Kept fully separate so export styling never drifts.

    public static ChartConfig WeeklyDark(StatsData data)
    {
        var series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = data.WeeklyMinutes,
                Name = "Focus minutes",
                Fill = null,
                GeometrySize = 8,
                Stroke = new SolidColorPaint(Sk("ChartWeekly"), 3),
                GeometryStroke = new SolidColorPaint(Sk("ChartWeekly"), 3),
                GeometryFill = new SolidColorPaint(Sk("VantaBg")),
            },
        };

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = data.WeekdayLabels,
                LabelsPaint = new SolidColorPaint(Sk("VantaTextMuted")),
                SeparatorsPaint = new SolidColorPaint(Sk("VantaRingTrack")),
            },
        };
        var yAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "Minutes",
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(Sk("VantaTextMuted")),
                NamePaint = new SolidColorPaint(Sk("VantaTextMuted")),
                SeparatorsPaint = new SolidColorPaint(Sk("VantaRingTrack")),
            },
        };
        return new ChartConfig(series, xAxes, yAxes);
    }

    public static ChartConfig PeakHourDark(StatsData data)
    {
        var series = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = data.HourlyMinutes,
                Name = "Minutes",
                Fill = new SolidColorPaint(Sk("ChartPeak")),
            },
        };

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "Minutes",
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(Sk("VantaTextMuted")),
                NamePaint = new SolidColorPaint(Sk("VantaTextMuted")),
                SeparatorsPaint = new SolidColorPaint(Sk("VantaRingTrack")),
            },
        };
        var yAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = data.HourLabels,
                LabelsPaint = new SolidColorPaint(Sk("VantaTextMuted")),
                SeparatorsPaint = new SolidColorPaint(Sk("VantaRingTrack")),
            },
        };
        return new ChartConfig(series, xAxes, yAxes);
    }
}
