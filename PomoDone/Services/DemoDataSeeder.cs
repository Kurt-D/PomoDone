using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.Services;

// Inserts ~6 weeks of plausible completed Focus sessions plus some ReviewLog
// rows so the charts and heatmap look alive at the defense (the real app will
// be days old). Debug-only entry point on StatsPage. Disclose if asked:
// "seeded data to demonstrate the analytics at scale."
public class DemoDataSeeder
{
    // The most recent LiveStreakDays days are seeded UNBROKEN and end on TODAY,
    // so the streak is live and long enough to cap freezes at 3 (21 / 7 = 3 → 3).
    // A single forced gap immediately older than the run bounds the streak at
    // exactly LiveStreakDays; older weeks keep realistic gaps. At 21 the live run
    // is ~3 weeks, so the heatmap still shows older gappy texture within view
    // while the recent streak demos the freeze cap.
    private const int LiveStreakDays = 21;
    private const int OlderGappyWeeks = 4;

    // Local hours a student plausibly studies, weighted toward a 9 PM peak.
    // The hour is picked at random from this list, so 21:00 dominates.
    private static readonly int[] HourPool =
    {
        9, 10, 14, 15, 16,
        19, 20,
        21, 21, 21, 21,   // 9 PM peak
        22, 22,
    };

    private readonly SessionRepository _sessions;
    private readonly ReviewLogRepository _reviews;

    public DemoDataSeeder(SessionRepository sessions, ReviewLogRepository reviews)
    {
        _sessions = sessions;
        _reviews = reviews;
    }

    public async Task GenerateAsync()
    {
        var random = new Random();
        var sessions = new List<Session>();
        var reviews = new List<ReviewLog>();

        var today = DateTime.Now.Date;
        // +1 for the forced boundary gap that bounds the live streak at exactly
        // LiveStreakDays, then the older textured weeks behind it.
        var totalDays = LiveStreakDays + 1 + OlderGappyWeeks * 7;

        for (var dayOffset = totalDays - 1; dayOffset >= 0; dayOffset--)
        {
            var localDay = today.AddDays(-dayOffset);

            // dayOffset 0..LiveStreakDays-1 == today back through the live streak:
            // always seeded (unbroken). dayOffset == LiveStreakDays is the forced
            // gap that ends the streak cleanly. Older days keep realistic gaps.
            var inLiveStreak = dayOffset < LiveStreakDays;
            if (dayOffset == LiveStreakDays)
                continue; // boundary gap → streak computes as exactly LiveStreakDays
            if (!inLiveStreak && random.NextDouble() < 0.25)
                continue; // realistic gaps: skip ~one day in four (older region only)

            // Upward trend: later weeks have more sessions per active day (bounded
            // so the recent live run stays a believable 1..3 sessions/day).
            var weekIndex = (totalDays - 1 - dayOffset) / 7;          // 0 (oldest) .. newest
            var baseSessions = 1 + Math.Min(weekIndex / 2, 2);        // 1..3
            var sessionsToday = baseSessions + random.Next(0, 2);     // + 0..1

            for (var i = 0; i < sessionsToday; i++)
            {
                var hour = HourPool[random.Next(HourPool.Length)];
                var minute = random.Next(0, 60);
                var localStart = localDay.AddHours(hour).AddMinutes(minute);

                // Stored as UTC — the single project-wide convention.
                var startUtc = localStart.ToUniversalTime();

                sessions.Add(new Session
                {
                    StartUtc = startUtc,
                    DurationMinutes = 25,
                    Type = SessionType.Focus,
                    Completed = true,
                    SecondsAway = random.Next(0, 120),
                });

                // Some breaks include a quick review burst.
                if (random.NextDouble() < 0.5)
                {
                    var reviewCount = random.Next(2, 6);
                    for (var r = 0; r < reviewCount; r++)
                    {
                        reviews.Add(new ReviewLog
                        {
                            FlashcardId = 1, // counts feed the stat; FK validity is irrelevant here
                            ReviewedUtc = startUtc.AddMinutes(25 + r),
                            WasCorrect = random.NextDouble() < 0.7,
                        });
                    }
                }
            }
        }

        await _sessions.InsertAllAsync(sessions);
        await _reviews.InsertAllAsync(reviews);
    }
}
