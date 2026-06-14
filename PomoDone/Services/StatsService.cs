using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.Services;

// Buckets completed Focus sessions into the three visuals' data. Sessions are
// stored UTC; everything here converts to LOCAL time before bucketing by day
// or hour, so an 11:30 PM session lands on the right day (CLAUDE.md 3.4).
public class StatsService
{
    private const int WeeklyDays = 7;

    private readonly SessionRepository _sessions;

    public StatsService(SessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<StatsData> BuildAsync()
    {
        var all = await _sessions.GetAllAsync();
        var focus = all
            .Where(s => s.Type == SessionType.Focus && s.Completed)
            .Select(s => new { Local = ToLocal(s.StartUtc), s.DurationMinutes })
            .ToList();

        return new StatsData
        {
            WeekdayLabels = BuildWeekdayLabels(),
            WeeklyMinutes = BuildWeeklyMinutes(focus.Select(f => (f.Local, f.DurationMinutes))),
            HourLabels = BuildHourLabels(),
            HourlyMinutes = BuildHourlyMinutes(focus.Select(f => (f.Local, f.DurationMinutes))),
            Heatmap = BuildHeatmap(focus.Select(f => (f.Local, f.DurationMinutes))),
            HasData = focus.Count > 0,
        };
    }

    private static string[] BuildWeekdayLabels()
    {
        var today = DateTime.Now.Date;
        var labels = new string[WeeklyDays];
        for (var i = 0; i < WeeklyDays; i++)
        {
            var day = today.AddDays(-(WeeklyDays - 1 - i));
            labels[i] = day.ToString("ddd"); // Mon, Tue, ...
        }
        return labels;
    }

    private static double[] BuildWeeklyMinutes(IEnumerable<(DateTime Local, int Minutes)> focus)
    {
        var today = DateTime.Now.Date;
        var minutes = new double[WeeklyDays];
        foreach (var (local, mins) in focus)
        {
            var dayIndex = WeeklyDays - 1 - (int)(today - local.Date).TotalDays;
            if (dayIndex is >= 0 and < WeeklyDays)
                minutes[dayIndex] += mins;
        }
        return minutes;
    }

    private static string[] BuildHourLabels()
    {
        var labels = new string[24];
        for (var h = 0; h < 24; h++)
            labels[h] = DateTime.Today.AddHours(h).ToString("h tt"); // 12 AM, 1 AM, ...
        return labels;
    }

    private static double[] BuildHourlyMinutes(IEnumerable<(DateTime Local, int Minutes)> focus)
    {
        var minutes = new double[24];
        foreach (var (local, mins) in focus)
            minutes[local.Hour] += mins;
        return minutes;
    }

    // Calendar grid for the current local month: leading placeholders to align
    // the 1st under its weekday, then one cell per day coloured by intensity.
    private static List<HeatCell> BuildHeatmap(IEnumerable<(DateTime Local, int Minutes)> focus)
    {
        var perDay = new Dictionary<DateTime, double>();
        foreach (var (local, mins) in focus)
        {
            var day = local.Date;
            perDay[day] = perDay.TryGetValue(day, out var v) ? v + mins : mins;
        }

        var today = DateTime.Now.Date;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var cells = new List<HeatCell>();

        // Leading blanks: Sunday-first grid (DayOfWeek Sunday == 0).
        for (var i = 0; i < (int)firstOfMonth.DayOfWeek; i++)
            cells.Add(new HeatCell { Intensity = -1 });

        for (var d = 0; d < daysInMonth; d++)
        {
            var day = firstOfMonth.AddDays(d);
            perDay.TryGetValue(day, out var mins);
            cells.Add(new HeatCell
            {
                Date = day,
                Minutes = mins,
                Intensity = IntensityFor(mins),
            });
        }

        return cells;
    }

    // Minutes -> 0..4 bucket. ~25 min == one session.
    private static int IntensityFor(double minutes) => minutes switch
    {
        <= 0 => 0,
        < 30 => 1,
        < 60 => 2,
        < 100 => 3,
        _ => 4,
    };

    private static DateTime ToLocal(DateTime storedUtc) =>
        DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc).ToLocalTime();
}
