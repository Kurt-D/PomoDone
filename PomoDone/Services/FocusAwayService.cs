using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.Services;

// Honor-system "focus purity" away-tracking (CLAUDE.md §3.3): measures the real
// wall-clock time the user spent OUTSIDE the app while a Focus session was
// running. Recorded and shown, never penalized.
//
// Timestamp-based, consistent with the wall-clock timer (§3.1) — never in-memory
// tick counting that dies with the process. Two pieces of persistent state, so
// away-time survives a kill mid-session:
//   • Accumulated away-seconds → Session.SecondsAway on the in-progress row (DB),
//     written through the existing single-connection repository (§3.4).
//   • The OPEN leave-instant (left, not yet returned) → ONE Preferences key as
//     UTC ticks. The frozen §5 schema has no column for an in-flight interval,
//     and theme + active-task already use Preferences for the same "survive a
//     kill" reason, so the open instant lives there (display-only, off-schema).
//
// Only Focus sessions are tracked; ShortBreak / LongBreak / idle are ignored.
// This service is the pure I/O shell around AwayTimeMath (the unit-tested pure
// math), mirroring the StreakMath / StreakFreezeService split.
public class FocusAwayService
{
    private const string AwaySinceKey = "focus_away_since_ticks";

    private readonly SessionRepository _sessions;

    public FocusAwayService(SessionRepository sessions) => _sessions = sessions;

    // App backgrounded: if a Focus session is in progress, stamp the instant the
    // user left. Keeps the FIRST stamp if Deactivated fires twice without an
    // intervening Activated (some platforms emit spurious lifecycle events).
    public async Task OnDeactivatedAsync()
    {
        if (Preferences.ContainsKey(AwaySinceKey))
            return;

        var session = await _sessions.GetInProgressAsync();
        if (session is { Type: SessionType.Focus })
            SetLeaveInstant(DateTime.UtcNow);
    }

    // App foregrounded: close an open leave interval onto the running Focus row.
    // The elapsed time-away is clamped to the session window, added to the row's
    // SecondsAway, and persisted; the open instant is then cleared. If there is no
    // running Focus session to attribute it to, the stale instant is dropped.
    public async Task OnActivatedAsync()
    {
        if (ReadLeaveInstant() is not DateTime leftUtc)
            return;

        var session = await _sessions.GetInProgressAsync();
        if (session is { Type: SessionType.Focus })
        {
            session.SecondsAway += SecondsAway(leftUtc, session);
            await _sessions.SaveAsync(session);
        }

        ClearLeaveInstant();
    }

    // Called by CompleteAsync BEFORE the completion write. The activation handler
    // writes SecondsAway straight to the row, so the VM's in-memory copy can be
    // stale — re-read the persisted value, then fold any STILL-open interval
    // (clamped to the session end) so a session that ends while the user is away
    // (e.g. killed-while-away, reopened after the end time) is not under-counted.
    // Pure read-modify on the passed object; the caller persists it.
    public async Task FinalizeOnCompleteAsync(Session finished)
    {
        var persisted = (await _sessions.GetByIdAsync(finished.Id))?.SecondsAway ?? finished.SecondsAway;

        if (ReadLeaveInstant() is DateTime leftUtc)
        {
            persisted += SecondsAway(leftUtc, finished);
            ClearLeaveInstant();
        }

        finished.SecondsAway = persisted;
    }

    // Cancel / new-session start: the in-progress row is deleted (cancel) or about
    // to be replaced (start), so NO SecondsAway is written — just drop any open
    // leave-instant so it can't bleed into the next session.
    public void Reset() => ClearLeaveInstant();

    // Whole away-seconds for [leftUtc, now], clamped to the session's UTC window.
    // The end instant comes from the ONE shared source (DebugTiming.SessionEndUtc),
    // so the away-clamp shrinks together with the countdown/alarm in seconds-mode.
    private static int SecondsAway(DateTime leftUtc, Session session) =>
        (int)Math.Round(
            AwayTimeMath.ClampedSeconds(
                leftUtc, DateTime.UtcNow, session.StartUtc, DebugTiming.SessionEndUtc(session)),
            MidpointRounding.AwayFromZero);

    private static DateTime? ReadLeaveInstant()
    {
        if (!Preferences.ContainsKey(AwaySinceKey))
            return null;
        var ticks = Preferences.Get(AwaySinceKey, 0L);
        return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
    }

    private static void SetLeaveInstant(DateTime utc) => Preferences.Set(AwaySinceKey, utc.Ticks);
    private static void ClearLeaveInstant() => Preferences.Remove(AwaySinceKey);
}
