using PomoDone.Models;

namespace PomoDone.Services;

// DEBUG-ONLY testing aid. When enabled, the app interprets a session's
// DurationMinutes NUMBER as SECONDS instead of minutes, so a "25" session runs
// ~25 seconds. This exercises the full wall-clock path — on-screen countdown,
// the AlarmManager exact alarm (§3.2), completion, resume-after-kill (§3.1), and
// the SecondsAway away-window clamp (§3.3) — without waiting real minutes.
//
// It NEVER touches stored data: DurationMinutes stays 25/5/15 (the §5 schema is
// unchanged) — only the RUNTIME unit changes. In Release the flag is a
// compile-time constant false, so the whole feature compiles out and production
// timing is byte-for-byte identical to before.
public static class DebugTiming
{
#if DEBUG
    private const string PreferenceKey = "debug_seconds_mode";

    // Default OFF: a normal debug run still uses real minutes until a tester opts
    // in via the TimerPage "DEBUG: seconds mode" switch. Backed by Preferences so
    // the choice SURVIVES PROCESS DEATH (same pattern as ThemeManager /
    // ActiveTaskService): on a notification-tap cold start the OS-held alarm
    // already fired at the seconds time, so the reopen path must recompute the
    // session end in seconds too — an in-memory flag would revert to false and
    // wrongly recompute it in minutes (session stuck at ~25:00, never completing).
    public static bool UseSecondsForTesting
    {
        get => Preferences.Get(PreferenceKey, false);
        set => Preferences.Set(PreferenceKey, value);
    }
#else
    public const bool UseSecondsForTesting = false;
#endif

    // Real seconds each duration "unit" represents: minutes normally, seconds
    // when the debug flag is on. The ONE conversion point for the whole app.
    public static double RealSecondsPerUnit => UseSecondsForTesting ? 1.0 : 60.0;

    // THE single source of truth for a session's end instant. The countdown,
    // alarm fire-time, completion trigger, resume recomputation, and the
    // away-window clamp all derive from this, so every one of them shrinks
    // together in seconds-mode (the alarm never fires at the real-minute time).
    public static DateTime SessionEndUtc(Session session) =>
        session.StartUtc.AddSeconds(session.DurationMinutes * RealSecondsPerUnit);
}
