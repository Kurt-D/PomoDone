using SQLite;

namespace PomoDone.Models;

// Exactly one row, always Id = 1. Points/streak/level/badges are derived from
// Session and ReviewLog rows at runtime — never stored here (CLAUDE.md §3.5).
//
// EXCEPTION (the one deliberate §3.5 carve-out): streak FREEZES. The streak
// LENGTH stays derived from Session rows; a freeze is a separate STORED
// consumable that patches a single missed day. A consumed freeze is real
// persistent state — it cannot be re-derived from sessions — so it lives here,
// on the single profile row, per §3.5's "only persistent profile state is the
// single-row UserProfile." Keep that split explicit: sessions = streak length;
// these three columns = the consumable + its bookkeeping.
public class UserProfile
{
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    public string? DisplayName { get; set; }

    public string? AvatarPath { get; set; }

    // Banked freezes, 0..3. Earned 1 per completed 7-day streak, capped at 3.
    public int FreezesAvailable { get; set; }

    // Idempotency marker: the gap-day a freeze has already patched. Holds a
    // LOCAL calendar date (a day bucket, not an instant) to match how focus
    // days are bucketed (§3.4 ToLocalDate); compared date-to-date, never
    // re-converted. Null when the current streak has no patched gap.
    public DateTime? LastFrozenDateUtc { get; set; }

    // How many freezes have ever been granted to the CURRENT streak (a count,
    // not a streak-day threshold). The earn pass grants eligible-minus-this each
    // time, so milestones never double-grant. Reset to 0 when the streak
    // genuinely breaks, so the next streak can earn fresh.
    public int FreezesEarnedTotal { get; set; }
}
