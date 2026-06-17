using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.Services;

// Single source of truth for gamification: everything is COMPUTED here from
// Session + ReviewLog rows at runtime. No points table, no streak table —
// see CLAUDE.md 3.5. Timestamps are stored UTC and converted to local time
// only here, for day bucketing (a 11:30 PM session must land on the right day).
public class GamificationService
{
    private const int PointsPerFocusSession = 10;
    private const int PointsPerReview = 2;

    // Cumulative point thresholds; Level = how many you've reached (min 1).
    private static readonly int[] LevelThresholds =
        { 0, 50, 120, 220, 350, 520, 740, 1020, 1380, 1840 };

    private readonly SessionRepository _sessions;
    private readonly ReviewLogRepository _reviews;
    private readonly UserProfileRepository _profiles;

    public GamificationService(
        SessionRepository sessions,
        ReviewLogRepository reviews,
        UserProfileRepository profiles)
    {
        _sessions = sessions;
        _reviews = reviews;
        _profiles = profiles;
    }

    public async Task<GamificationSummary> ComputeAsync()
    {
        var allSessions = await _sessions.GetAllAsync();
        var reviews = await _reviews.GetAllAsync();
        var profile = await _profiles.GetAsync();

        var completedFocus = allSessions
            .Where(s => s.Type == SessionType.Focus && s.Completed)
            .ToList();

        var focusDays = completedFocus
            .Select(s => StreakMath.ToLocalDate(s.StartUtc))
            .ToHashSet();

        var points = completedFocus.Count * PointsPerFocusSession
                     + reviews.Count * PointsPerReview;

        // Streak LENGTH is still derived from sessions; the freeze is only a
        // single stored consumable (UserProfile.LastFrozenDateUtc) that bridges
        // one patched gap-day. Reading that marker here is a pure read — it
        // never writes. All freeze writes live in StreakFreezeService (§3.5).
        var frozenDay = profile?.LastFrozenDateUtc?.Date;
        var streak = StreakMath.ComputeStreak(focusDays, frozenDay, DateTime.Now.Date);

        var level = LevelThresholds.Count(t => points >= t);
        if (level < 1) level = 1;

        return new GamificationSummary
        {
            Points = points,
            Streak = streak,
            Level = level,
            DaysActive = focusDays.Count,
            CompletedFocusSessions = completedFocus.Count,
            ReviewCount = reviews.Count,
            ReviewsThisWeek = CountReviewsThisWeek(reviews),
            FocusPurityPercent = ComputeFocusPurity(completedFocus),
            FreezesAvailable = profile?.FreezesAvailable ?? 0,
            Badges = BuildBadges(completedFocus.Count, streak, focusDays.Count, reviews.Count),
        };
    }

    // Focused time vs. time spent away (lifecycle-tracked into SecondsAway).
    // Recorded and shown, never penalized — the "focus purity" metric.
    private static double ComputeFocusPurity(IReadOnlyCollection<Session> completedFocus)
    {
        var focusSeconds = completedFocus.Sum(s => (long)s.DurationMinutes * 60);
        if (focusSeconds == 0)
            return 100;

        var awaySeconds = completedFocus.Sum(s => (long)s.SecondsAway);
        var purity = (1 - (double)awaySeconds / focusSeconds) * 100;
        return Math.Clamp(purity, 0, 100);
    }

    private static List<Badge> BuildBadges(int focusCount, int streak, int daysActive, int reviewCount) =>
        new()
        {
            new Badge("First Focus", "Complete your first focus session", focusCount >= 1),
            new Badge("Getting Started", "Complete 10 focus sessions", focusCount >= 10),
            new Badge("Half Century", "Complete 50 focus sessions", focusCount >= 50),
            new Badge("On a Roll", "Reach a 3-day streak", streak >= 3),
            new Badge("Week Warrior", "Reach a 7-day streak", streak >= 7),
            new Badge("Consistent", "Focus on 14 different days", daysActive >= 14),
            new Badge("Reviewer", "Review a flashcard on a break", reviewCount >= 1),
            new Badge("Study Buddy", "Review 50 flashcards", reviewCount >= 50),
        };

    // "Cards reviewed during breaks this week." Counted STANDALONE from
    // ReviewLog by ReviewedUtc — NO join to Flashcard — so orphaned logs
    // (Option A: card/deck deleted, log retained) still count. Stored UTC is
    // converted to local before comparing to the local week boundary (3.4).
    private static int CountReviewsThisWeek(IReadOnlyCollection<ReviewLog> reviews)
    {
        var startOfWeek = StartOfWeekLocal();
        return reviews.Count(r => ToLocal(r.ReviewedUtc) >= startOfWeek);
    }

    // Start of the current local week, Sunday 00:00 (matches the heatmap grid).
    private static DateTime StartOfWeekLocal()
    {
        var today = DateTime.Now.Date;
        var daysSinceSunday = (int)today.DayOfWeek; // Sunday == 0
        return today.AddDays(-daysSinceSunday);
    }

    // Pin SQLite's Kind=Unspecified rows to UTC before converting, or ToLocalTime
    // would assume local and shift the day. (Day-bucket conversion lives in
    // StreakMath.ToLocalDate; this is the instant form used for week boundaries.)
    private static DateTime ToLocal(DateTime storedUtc) =>
        DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc).ToLocalTime();
}
