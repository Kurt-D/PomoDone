namespace PomoDone.Services;

// Pure, dependency-free auto-stop decision for the self-played session-end ring
// (mirrors AwayTimeMath / StreakMath / StudyImpact). The ring MUST NEVER loop
// forever, so it self-stops once it has played for maxRingSeconds — even if no UI
// is ever shown (app killed while ringing). Keeping the rule here makes it
// unit-testable without MAUI or an audio device. All instants are UTC.
public static class AlarmRingMath
{
    // Max time the session-end ring is allowed to play before forcing a stop.
    public const int DefaultMaxRingSeconds = 60;

    // True once the ring has played for at least maxRingSeconds. `now` is injected
    // (never DateTime.Now) so callers and tests are deterministic.
    public static bool ShouldStop(DateTime ringStartedUtc, DateTime nowUtc, int maxRingSeconds)
        => (nowUtc - ringStartedUtc).TotalSeconds >= maxRingSeconds;
}
