using PomoDone.Models;

namespace PomoDone.Services;

// Dependency-free streak + streak-freeze math. Pure functions only — no DB, no
// MAUI, no I/O — so the streak-freeze rules (the ONE deliberate §3.5 exception)
// can be unit-tested directly, and so the same logic is shared by the read path
// (GamificationService) and the write path (StreakFreezeService).
//
// THE §3.5 SPLIT, restated where it lives:
//   • ComputeStreak is a PURE READ of Session-derived focus days (+ at most one
//     freeze-bridged gap). It never writes. The streak LENGTH stays derived.
//   • Evaluate is the only place freeze STATE changes (consume/earn/reset). It
//     mutates a UserProfile in memory; persistence is the caller's job. A
//     consumed/earned freeze is real stored state that cannot be re-derived from
//     sessions — hence it lives on the single UserProfile row.
public static class StreakMath
{
    public const int MaxFreezes = 3;
    public const int EarnEveryStreakDays = 7;

    // Rows come back from SQLite with Kind=Unspecified; pin them to UTC before
    // converting, or ToLocalTime would assume local and shift the day. One
    // convention for every day bucket in the app (§3.4).
    public static DateTime ToLocalDate(DateTime storedUtc) =>
        DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc).ToLocalTime().Date;

    // Consecutive days ending at `today` with >= 1 completed Focus session.
    // Today not yet having one doesn't break the streak — it's only broken once a
    // full day with no focus session has passed. frozenLocalDate is the single
    // freeze-patched gap (StreakFreezeService owns setting it); a day counts as
    // covered if it has a focus session OR equals that one bridged date. Pass
    // null for the raw, session-only streak.
    //
    // PURE READ — no writes. `today` is injected (not DateTime.Now) so callers
    // and tests are deterministic.
    public static int ComputeStreak(HashSet<DateTime> focusDays, DateTime? frozenLocalDate, DateTime today)
    {
        if (focusDays.Count == 0)
            return 0;

        bool Covered(DateTime day) => focusDays.Contains(day) || day == frozenLocalDate;

        var cursor = Covered(today) ? today : today.AddDays(-1);

        var streak = 0;
        while (Covered(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    // The streak-freeze decision: gap-detect → CONSUME (first) → EARN (second),
    // off the surviving streak. Mutates `profile` in place; returns whether
    // anything changed (so the caller can skip an unnecessary write). Idempotent:
    // safe to run on every app open (same-day re-runs neither double-consume nor
    // double-grant). `focusDays` are already local day buckets; `today` is injected.
    public static bool Evaluate(HashSet<DateTime> focusDays, UserProfile profile, DateTime today)
    {
        if (focusDays.Count == 0)
            return false;

        var dirty = false;
        var lastFocusDay = focusDays.Max();
        var daysSinceLastFocus = (today - lastFocusDay).Days;

        // ---- CONSUME (first) ----
        // Exactly one fully-passed missed day exists when the last focus day was
        // the day before yesterday (today is still in its grace period):
        //   0 -> focused today (no gap)   1 -> yesterday, today is grace (no gap)
        //   2 -> ONE missed day = gap     >=3 -> two+ missed days = hard break
        if (daysSinceLastFocus == 2)
        {
            var gapDay = lastFocusDay.AddDays(1); // == today.AddDays(-1)

            if (profile.LastFrozenDateUtc?.Date == gapDay)
            {
                // Already patched this exact gap (idempotency): a same-day re-open
                // must not drain a second freeze. Do nothing.
            }
            else if (profile.FreezesAvailable > 0)
            {
                // Spend one freeze to patch the single gap-day; streak survives.
                profile.LastFrozenDateUtc = gapDay;
                profile.FreezesAvailable -= 1;
                dirty = true;
            }
            else
            {
                // One-day gap but no freeze to spend → streak genuinely breaks.
                dirty |= ResetOnBreak(profile);
            }
        }
        else if (daysSinceLastFocus >= 3)
        {
            // Two or more consecutive missed days break the streak even with
            // freezes in the bank (a freeze patches only a SINGLE day).
            dirty |= ResetOnBreak(profile);
        }
        // daysSinceLastFocus 0 or 1 → streak intact; any existing marker still
        // bridges its gap within the current run, so leave it untouched.

        // ---- EARN (second), off the SURVIVING streak ----
        // Count model: eligible = how many freezes a streak of this length is
        // worth (1 per 7 days), capped at MaxFreezes. Grant the difference vs.
        // however many have already been granted (FreezesEarnedTotal). This
        // awards ALL thresholds crossed in one pass (a fully-formed 45-day streak
        // grants 3 at once), is idempotent (eligible == earned → no-op), and is
        // cap-safe (both sides clamped to MaxFreezes).
        var streak = ComputeStreak(focusDays, profile.LastFrozenDateUtc?.Date, today);
        var eligible = Math.Min(MaxFreezes, streak / EarnEveryStreakDays);
        if (eligible > profile.FreezesEarnedTotal)
        {
            var toGrant = eligible - profile.FreezesEarnedTotal;
            profile.FreezesAvailable = Math.Min(MaxFreezes, profile.FreezesAvailable + toGrant);
            profile.FreezesEarnedTotal = eligible;
            dirty = true;
        }

        return dirty;
    }

    // A genuine break starts the next streak clean: drop the stale gap marker so
    // it can't block a future legit patch, and reset the earned count so the next
    // streak can earn from scratch. Banked FreezesAvailable are NOT touched — a
    // two-day gap breaks the streak but you keep what you earned. Returns whether
    // anything changed.
    public static bool ResetOnBreak(UserProfile profile)
    {
        if (profile.LastFrozenDateUtc is null && profile.FreezesEarnedTotal == 0)
            return false;

        profile.LastFrozenDateUtc = null;
        profile.FreezesEarnedTotal = 0;
        return true;
    }
}
