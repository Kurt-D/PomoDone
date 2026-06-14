using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly StatsService _stats;
    private readonly DemoDataSeeder _seeder;
    private readonly IChartExportService _export;

    // The raw buckets behind the current charts; reused to render the export.
    private StatsData _data = new();

    [ObservableProperty]
    private ISeries[] _weeklySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ICartesianAxis[] _weeklyXAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private ICartesianAxis[] _weeklyYAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private ISeries[] _peakSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ICartesianAxis[] _peakXAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private ICartesianAxis[] _peakYAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<HeatCell> Heatmap { get; } = new();

    // Gates the "Generate Demo Data" button to debug builds only.
    public bool IsDebugBuild
    {
#if DEBUG
        get => true;
#else
        get => false;
#endif
    }

    public StatsViewModel(StatsService stats, DemoDataSeeder seeder, IChartExportService export)
    {
        _stats = stats;
        _seeder = seeder;
        _export = export;
    }

    public async Task LoadAsync()
    {
        _data = await _stats.BuildAsync();
        HasData = _data.HasData;

        var weekly = StatsChartFactory.Weekly(_data);
        WeeklySeries = weekly.Series;
        WeeklyXAxes = weekly.XAxes;
        WeeklyYAxes = weekly.YAxes;

        var peak = StatsChartFactory.PeakHour(_data);
        PeakSeries = peak.Series;
        PeakXAxes = peak.XAxes;
        PeakYAxes = peak.YAxes;

        Heatmap.Clear();
        foreach (var cell in _data.Heatmap)
            Heatmap.Add(cell);
    }

    [RelayCommand]
    private async Task GenerateDemoDataAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Generating demo data…";
        await _seeder.GenerateAsync();
        await LoadAsync();
        StatusMessage = "Demo data generated.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ExportChartsAsync()
    {
        if (IsBusy)
            return;

        if (!HasData)
        {
            StatusMessage = "No data to export yet.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Exporting charts…";
        try
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Build fresh series for the headless render so we never hand the
            // on-screen chart's live series to a second chart.
            var weeklyPng = ChartImageRenderer.ToPng(StatsChartFactory.Weekly(_data));
            var peakPng = ChartImageRenderer.ToPng(StatsChartFactory.PeakHour(_data));

            var savedWeekly = await _export.SaveImageAsync($"pomodone_weekly_{stamp}.png", weeklyPng);
            var savedPeak = await _export.SaveImageAsync($"pomodone_peakhour_{stamp}.png", peakPng);

            StatusMessage = savedWeekly && savedPeak
                ? "Saved to Pictures/Pomodone."
                : "Export failed.";
        }
        catch (Exception)
        {
            StatusMessage = "Export failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
