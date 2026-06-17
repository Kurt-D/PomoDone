using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.Services;

// The streak-freeze STARTUP PASS — the one deliberate exception to CLAUDE.md
// §3.5 (gamification is otherwise derived, never stored).
//
// THE SPLIT (defend this at the panel):
//   • Streak LENGTH stays derived from Session rows. StreakMath.ComputeStreak is
//     a PURE READ and performs no writes.
//   • A streak FREEZE is a separate STORED consumable on the single UserProfile
//     row. Consuming/earning one is real persistent state that cannot be
//     re-derived from sessions, so the decision lives in StreakMath.Evaluate and
//     this service is the only thing that PERSISTS the result, once per app open.
//
// This class is pure I/O: load sessions + profile, hand the decision to the pure
// StreakMath.Evaluate (unit-tested without DB/platform), persist if it changed.
public class StreakFreezeService
{
    private readonly SessionRepository _sessions;
    private readonly UserProfileRepository _profiles;

    public StreakFreezeService(SessionRepository sessions, UserProfileRepository profiles)
    {
        _sessions = sessions;
        _profiles = profiles;
    }

    public async Task EvaluateAsync()
    {
        var sessions = await _sessions.GetAllAsync();

        // Same UTC→local day bucketing as the derived stats (§3.4) — a near-
        // midnight session must land on the correct local day.
        var focusDays = sessions
            .Where(s => s.Type == SessionType.Focus && s.Completed)
            .Select(s => StreakMath.ToLocalDate(s.StartUtc))
            .ToHashSet();

        if (focusDays.Count == 0)
            return; // no streak to protect or grow

        var profile = await _profiles.GetAsync() ?? new UserProfile();

        var dirty = StreakMath.Evaluate(focusDays, profile, DateTime.Now.Date);
        if (dirty)
            await _profiles.SaveAsync(profile);
    }
}
