using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace PomoDone.Services;

// Renders a chart to PNG bytes WITHOUT touching the on-screen view: a headless
// in-memory SKCartesianChart built from the same series/axes, then encoded.
// No screenshots — see CLAUDE.md 4.4. Cross-platform (pure SkiaSharp); only
// the final byte save is platform-specific (IChartExportService).
public static class ChartImageRenderer
{
    public static byte[] ToPng(ChartConfig config, int width = 1000, int height = 520)
    {
        var chart = new SKCartesianChart
        {
            Width = width,
            Height = height,
            Series = config.Series,
            XAxes = config.XAxes,
            YAxes = config.YAxes,
            Background = SKColors.White,
        };

        using var image = chart.GetImage();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
