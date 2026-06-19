namespace PomoDone.Services;

// Pure, dependency-free focus-purity math (mirrors AwayTimeMath / StreakMath /
// StudyImpact) so the denominator decision is unit-testable without MAUI or a DB.
//
// Focus purity = the fraction of a session's focus WINDOW the user spent IN the
// app, as a 0..100 percent. The honor-system metric (§3.3): recorded and shown,
// never penalized.
//
// The window length is DurationMinutes scaled by the SAME unit the rest of the
// timing chain uses — realSecondsPerUnit is 60 in normal minutes-mode and 1 in
// the DEBUG seconds-mode that DebugTiming exposes. The scale is PASSED IN (callers
// supply DebugTiming.RealSecondsPerUnit; tests supply it directly) so this stays
// pure and so no mode check leaks into the math. In release that scale is the
// compile-time const 60, making FocusWindowSeconds byte-identical to the original
// DurationMinutes * 60.
public static class FocusPurityMath
{
    // Seconds in one session's focus window for the active unit scale.
    public static long FocusWindowSeconds(int durationMinutes, double realSecondsPerUnit)
        => (long)Math.Round(durationMinutes * realSecondsPerUnit, MidpointRounding.AwayFromZero);

    // Purity percent (0..100) from total focus-window seconds and total away-seconds.
    // A non-positive window reads as 100% (nothing to dilute — matches prior behaviour).
    public static double Percent(long focusSeconds, long awaySeconds)
    {
        if (focusSeconds <= 0)
            return 100;

        var purity = (1 - (double)awaySeconds / focusSeconds) * 100;
        return Math.Clamp(purity, 0, 100);
    }
}
