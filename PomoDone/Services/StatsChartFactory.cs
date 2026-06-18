using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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
    private static readonly SKColor Primary = new(0x51, 0x2B, 0xD4);

    // On-screen dark-theme paints (match the Vanta tokens; the on-screen chart
    // canvas is transparent and shows the dark page/card behind it).
    private static readonly SKColor Accent = new(0xF5, 0x9E, 0x0B);   // VantaAccent
    private static readonly SKColor Bg = new(0x11, 0x11, 0x11);       // VantaBg (hollow point centers)
    private static readonly SKColor AxisText = new(0x8A, 0x8A, 0x8A); // VantaTextMuted
    private static readonly SKColor Separator = new(0x22, 0x22, 0x22);// VantaRingTrack

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
                Stroke = new SolidColorPaint(Accent, 3),
                GeometryStroke = new SolidColorPaint(Accent, 3),
                GeometryFill = new SolidColorPaint(Bg),
            },
        };

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = data.WeekdayLabels,
                LabelsPaint = new SolidColorPaint(AxisText),
                SeparatorsPaint = new SolidColorPaint(Separator),
            },
        };
        var yAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "Minutes",
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(AxisText),
                NamePaint = new SolidColorPaint(AxisText),
                SeparatorsPaint = new SolidColorPaint(Separator),
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
                Fill = new SolidColorPaint(Accent),
            },
        };

        var xAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Name = "Minutes",
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(AxisText),
                NamePaint = new SolidColorPaint(AxisText),
                SeparatorsPaint = new SolidColorPaint(Separator),
            },
        };
        var yAxes = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = data.HourLabels,
                LabelsPaint = new SolidColorPaint(AxisText),
                SeparatorsPaint = new SolidColorPaint(Separator),
            },
        };
        return new ChartConfig(series, xAxes, yAxes);
    }
}
