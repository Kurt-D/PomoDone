using PomoDone.Services;
using Xunit;

namespace PomoDone.Tests;

// Pure unit tests for the derived Study Impact analytics. No DB, no MAUI —
// StudyImpact takes plain ReviewEntry lists with LOCAL instants and an injected
// `today`, so every case is deterministic (mirrors StreakMathTests).
public class StudyImpactTests
{
    // Wednesday. This week's Sunday = 2026-06-14; last week's Sunday = 2026-06-07.
    private static readonly DateTime Today = new(2026, 6, 17);
    private static readonly DateTime ThisWeek = new(2026, 6, 15); // Mon, this week
    private static readonly DateTime LastWeek = new(2026, 6, 9);  // Tue, last week

    private static ReviewEntry Entry(int card, DateTime when, bool correct) => new(card, when, correct);

    [Fact]
    public void EmptyData_AllZero_NoFlags()
    {
        var r = StudyImpact.Compute(Array.Empty<ReviewEntry>(), Today);

        Assert.Equal(0, r.ReviewedThisWeek);
        Assert.Equal(0, r.RecoveredCards);
        Assert.False(r.HasThisWeek);
        Assert.False(r.HasLastWeek);
        Assert.Equal(0, r.AccuracyThisWeekPercent);
        Assert.Equal(0, r.AccuracyLastWeekPercent);
    }

    [Fact]
    public void MissedThenCorrect_CountsAsRecovered()
    {
        var entries = new[]
        {
            Entry(1, LastWeek.AddHours(20), false), // missed earlier
            Entry(1, ThisWeek.AddHours(20), true),  // correct later
        };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(1, r.RecoveredCards);
    }

    [Fact]
    public void AlwaysCorrect_NotRecovered()
    {
        var entries = new[]
        {
            Entry(2, LastWeek.AddHours(20), true),
            Entry(2, ThisWeek.AddHours(20), true),
        };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(0, r.RecoveredCards);
    }

    [Fact]
    public void MissedAndStillMissed_NotRecovered()
    {
        var entries = new[]
        {
            Entry(3, LastWeek.AddHours(20), false),
            Entry(3, ThisWeek.AddHours(20), false),
        };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(0, r.RecoveredCards);
    }

    [Fact]
    public void CorrectThenMissed_NotRecovered()
    {
        // Right first, then missed later → a regression, not a recovery.
        var entries = new[]
        {
            Entry(4, LastWeek.AddHours(20), true),
            Entry(4, ThisWeek.AddHours(20), false),
        };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(0, r.RecoveredCards);
    }

    [Fact]
    public void WeekBoundary_BucketsThisVsLastWeek()
    {
        var entries = new[]
        {
            // last week: 1 correct, 1 wrong → 50%
            Entry(10, LastWeek.AddHours(20), true),
            Entry(11, LastWeek.AddHours(21), false),
            // this week: 3 correct, 1 wrong → 75%
            Entry(12, ThisWeek.AddHours(20), true),
            Entry(13, ThisWeek.AddHours(21), true),
            Entry(14, ThisWeek.AddHours(22), true),
            Entry(15, ThisWeek.AddHours(23), false),
        };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(4, r.ReviewedThisWeek);
        Assert.True(r.HasThisWeek);
        Assert.True(r.HasLastWeek);
        Assert.Equal(75, r.AccuracyThisWeekPercent);
        Assert.Equal(50, r.AccuracyLastWeekPercent);
    }

    [Fact]
    public void OlderThanLastWeek_ExcludedFromBothWindows()
    {
        var twoWeeksAgo = new DateTime(2026, 6, 1);
        var entries = new[] { Entry(20, twoWeeksAgo.AddHours(20), true) };

        var r = StudyImpact.Compute(entries, Today);

        Assert.Equal(0, r.ReviewedThisWeek);
        Assert.False(r.HasThisWeek);
        Assert.False(r.HasLastWeek);
    }
}
