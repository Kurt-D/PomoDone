using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using PomoDone.Models;
using PomoDone.Repositories;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly StatsService _stats;
    private readonly DemoDataSeeder _seeder;
    private readonly IChartExportService _export;
    private readonly StreakFreezeService _freeze;
    private readonly ReviewLogRepository _reviews;

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

    // Study Impact — derived purely from ReviewLog rows (§3.5). Display-only;
    // NOT part of the PNG export (§4.4 keeps the two LiveCharts2 charts only).
    [ObservableProperty]
    private int _recoveredCards;

    [ObservableProperty]
    private int _reviewedThisWeek;

    [ObservableProperty]
    private string _accuracyThisWeekDisplay = "—";

    [ObservableProperty]
    private string _accuracyLastWeekDisplay = "—";

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

    public StatsViewModel(StatsService stats, DemoDataSeeder seeder, IChartExportService export, StreakFreezeService freeze, ReviewLogRepository reviews)
    {
        _stats = stats;
        _seeder = seeder;
        _export = export;
        _freeze = freeze;
        _reviews = reviews;
    }

    public async Task LoadAsync()
    {
        _data = await _stats.BuildAsync();
        HasData = _data.HasData;

        // On-screen charts use the dark-theme styling (amber series, light axis
        // text). The EXPORT path (ExportChartsAsync) still calls the unchanged
        // Weekly/PeakHour so the saved PNG keeps its original styling (§4.4).
        var weekly = StatsChartFactory.WeeklyDark(_data);
        WeeklySeries = weekly.Series;
        WeeklyXAxes = weekly.XAxes;
        WeeklyYAxes = weekly.YAxes;

        var peak = StatsChartFactory.PeakHourDark(_data);
        PeakSeries = peak.Series;
        PeakXAxes = peak.XAxes;
        PeakYAxes = peak.YAxes;

        Heatmap.Clear();
        foreach (var cell in _data.Heatmap)
            Heatmap.Add(cell);

        await LoadStudyImpactAsync();
    }

    // Derived Study Impact numbers from ReviewLog rows. Stored UTC is converted
    // to LOCAL here (§3.4) before the pure StudyImpact analyzer buckets by week;
    // `DateTime.Now.Date` is the injected "today". Reuses the existing repository
    // (singleton connection, §3.4) — no new DB connection. Excluded from export.
    private async Task LoadStudyImpactAsync()
    {
        var logs = await _reviews.GetAllAsync();
        var entries = logs.Select(r => new ReviewEntry(
            r.FlashcardId,
            DateTime.SpecifyKind(r.ReviewedUtc, DateTimeKind.Utc).ToLocalTime(),
            r.WasCorrect));

        var impact = StudyImpact.Compute(entries, DateTime.Now.Date);
        RecoveredCards = impact.RecoveredCards;
        ReviewedThisWeek = impact.ReviewedThisWeek;
        AccuracyThisWeekDisplay = impact.HasThisWeek ? $"{impact.AccuracyThisWeekPercent}%" : "—";
        AccuracyLastWeekDisplay = impact.HasLastWeek ? $"{impact.AccuracyLastWeekPercent}%" : "—";
    }

    // Re-resolve theme-dependent colours WITHOUT a DB round-trip: rebuild the
    // on-screen chart series/axes from the cached buckets and re-emit the
    // heatmap cells so the IntensityToColorConverter re-runs in the new theme.
    // Called from StatsPage on RequestedThemeChanged. The EXPORT path
    // (Weekly/PeakHour + ChartImageRenderer) is untouched.
    public void RefreshTheme()
    {
        var weekly = StatsChartFactory.WeeklyDark(_data);
        WeeklySeries = weekly.Series;
        WeeklyXAxes = weekly.XAxes;
        WeeklyYAxes = weekly.YAxes;

        var peak = StatsChartFactory.PeakHourDark(_data);
        PeakSeries = peak.Series;
        PeakXAxes = peak.XAxes;
        PeakYAxes = peak.YAxes;

        var cells = _data.Heatmap;
        Heatmap.Clear();
        foreach (var cell in cells)
            Heatmap.Add(cell);
    }

    // Preset demo seeders. streakDaysText comes from each button's
    // CommandParameter ("6" / "7" / "21"). Order matters: GenerateAsync wipes
    // Session + ReviewLog then reseeds the fixed-length streak; then the freeze
    // columns are reset (StreakFreezeService owns them — kept OUT of the seeder);
    // then refresh. The startup pass / ProfilePage load re-earns the correct
    // freeze count off the fresh streak, so streak and freezes always agree.
    [RelayCommand]
    private async Task GenerateDemoDataAsync(string streakDaysText)
    {
        if (IsBusy)
            return;

        if (!int.TryParse(streakDaysText, out var streakDays))
            return;

        IsBusy = true;
        StatusMessage = "Generating demo data…";
        await _seeder.GenerateAsync(streakDays);
        await _freeze.ResetFreezeStateAsync();
        await LoadAsync();
        StatusMessage = $"Seeded a {streakDays}-day streak.";
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
