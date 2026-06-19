namespace PomoDone.Services;

// Pure, dependency-free away-time math (mirrors StreakMath / StudyImpact) so it
// can be unit-tested without MAUI or a database. All instants are UTC.
//
// An away interval is the span between the user LEAVING the app (LeftUtc) and
// RETURNING (ReturnedUtc) while a Focus session is running. Seconds are clamped
// to the session's [StartUtc, EndUtc] window so time before the session began or
// after it ended can never inflate the honor-system focus-purity metric (§3.3).
public readonly record struct AwayInterval(DateTime LeftUtc, DateTime ReturnedUtc);

public static class AwayTimeMath
{
    // Total away-seconds across all intervals, each clamped to the session window.
    public static int TotalAwaySeconds(
        IEnumerable<AwayInterval> intervals, DateTime sessionStartUtc, DateTime sessionEndUtc)
    {
        var total = 0.0;
        foreach (var iv in intervals)
            total += ClampedSeconds(iv.LeftUtc, iv.ReturnedUtc, sessionStartUtc, sessionEndUtc);

        return (int)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    // Seconds of a single [left, returned] interval that fall inside [start, end].
    // A non-overlapping or inverted interval contributes 0. Comparisons are on
    // raw ticks, so it is correct whether the inputs carry Kind=Utc (DateTime.UtcNow)
    // or Kind=Unspecified (UTC ticks loaded from SQLite) — both hold UTC ticks.
    public static double ClampedSeconds(
        DateTime leftUtc, DateTime returnedUtc, DateTime sessionStartUtc, DateTime sessionEndUtc)
    {
        var from = Max(leftUtc, sessionStartUtc);
        var to = Min(returnedUtc, sessionEndUtc);
        var seconds = (to - from).TotalSeconds;
        return seconds > 0 ? seconds : 0;
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
