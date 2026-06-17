using PomoDone.Models;
using PomoDone.Services;
using Xunit;

namespace PomoDone.Tests;

// Pure unit tests for the streak-freeze decision logic (the one deliberate §3.5
// exception). No DB, no MAUI — StreakMath is dependency-free and `today` is
// injected, so every case is deterministic.
public class StreakMathTests
{
    private static readonly DateTime Today = new(2026, 6, 17);

    // `count` consecutive day buckets ending at `lastDay` (inclusive).
    private static HashSet<DateTime> DaysEndingAt(DateTime lastDay, int count) =>
        Enumerable.Range(0, count).Select(i => lastDay.AddDays(-i)).ToHashSet();

    [Fact]
    public void FortyFiveDayStreak_GrantsThree_InOnePass()
    {
        var focusDays = DaysEndingAt(Today, 45);
        var profile = new UserProfile();

        var dirty = StreakMath.Evaluate(focusDays, profile, Today);

        Assert.True(dirty);
        Assert.Equal(3, profile.FreezesAvailable);
        Assert.Equal(3, profile.FreezesEarnedTotal);
    }

    [Fact]
    public void Rerun_SameState_NoAdditionalGrant()
    {
        var focusDays = DaysEndingAt(Today, 45);
        var profile = new UserProfile();
        StreakMath.Evaluate(focusDays, profile, Today); // first pass → 3

        var dirty = StreakMath.Evaluate(focusDays, profile, Today); // idempotent re-run

        Assert.False(dirty);
        Assert.Equal(3, profile.FreezesAvailable);
        Assert.Equal(3, profile.FreezesEarnedTotal);
    }

    [Theory]
    [InlineData(14, 2)]
    [InlineData(7, 1)]
    [InlineData(6, 0)]
    public void StreakLength_GrantsExpectedFreezes(int streakDays, int expected)
    {
        var focusDays = DaysEndingAt(Today, streakDays);
        var profile = new UserProfile();

        StreakMath.Evaluate(focusDays, profile, Today);

        Assert.Equal(expected, profile.FreezesAvailable);
        Assert.Equal(expected, profile.FreezesEarnedTotal);
    }

    [Fact]
    public void SeventyDayStreak_CapsAtThree()
    {
        var focusDays = DaysEndingAt(Today, 70);
        var profile = new UserProfile();

        StreakMath.Evaluate(focusDays, profile, Today);

        Assert.Equal(3, profile.FreezesAvailable);
        Assert.Equal(3, profile.FreezesEarnedTotal); // clamps at MaxFreezes
    }

    [Fact]
    public void Break_ResetsEarnState_ThenFreshStreakReEarns()
    {
        // Last focus day was 3 days ago → two+ fully-passed missed days = break.
        var brokenProfile = new UserProfile
        {
            FreezesAvailable = 2,
            FreezesEarnedTotal = 3,
            LastFrozenDateUtc = Today.AddDays(-10),
        };
        var brokenDays = DaysEndingAt(Today.AddDays(-3), 5);

        StreakMath.Evaluate(brokenDays, brokenProfile, Today);

        Assert.Equal(0, brokenProfile.FreezesEarnedTotal);  // earn count reset
        Assert.Null(brokenProfile.LastFrozenDateUtc);        // marker cleared
        Assert.Equal(2, brokenProfile.FreezesAvailable);     // banked freezes survive a break

        // A fresh 7-day streak ending today, on the reset profile, re-earns 1.
        var freshDays = DaysEndingAt(Today, 7);
        StreakMath.Evaluate(freshDays, brokenProfile, Today);

        Assert.Equal(1, brokenProfile.FreezesEarnedTotal);
        Assert.Equal(3, brokenProfile.FreezesAvailable);     // 2 banked + 1 newly earned
    }

    [Fact]
    public void OneDayGap_WithFreezeInBank_ConsumesAndStreakSurvives()
    {
        // 7-day run ending the day before yesterday: today is grace, yesterday is
        // the single missed gap-day.
        var focusDays = DaysEndingAt(Today.AddDays(-2), 7);
        var profile = new UserProfile
        {
            FreezesAvailable = 2,
            FreezesEarnedTotal = 3, // already capped so EARN can't perturb the count
        };

        var dirty = StreakMath.Evaluate(focusDays, profile, Today);

        Assert.True(dirty);
        Assert.Equal(1, profile.FreezesAvailable);                  // 2 − 1 consumed
        Assert.Equal(Today.AddDays(-1), profile.LastFrozenDateUtc);  // gap = yesterday
        // Streak survives via the bridge: 7-day run + the patched gap = 8.
        Assert.Equal(8, StreakMath.ComputeStreak(focusDays, profile.LastFrozenDateUtc?.Date, Today));
    }

    [Fact]
    public void ConsumeIdempotency_RerunSameDay_NoSecondDecrement()
    {
        var focusDays = DaysEndingAt(Today.AddDays(-2), 7);
        var profile = new UserProfile { FreezesAvailable = 2, FreezesEarnedTotal = 3 };
        StreakMath.Evaluate(focusDays, profile, Today); // consume once → 1

        var dirty = StreakMath.Evaluate(focusDays, profile, Today); // same-day re-run

        Assert.False(dirty);
        Assert.Equal(1, profile.FreezesAvailable);                  // no second decrement
        Assert.Equal(Today.AddDays(-1), profile.LastFrozenDateUtc);
    }

    [Fact]
    public void TwoDayGap_BreaksDespiteFreezes_BothResetFieldsCleared()
    {
        // Last focus day 3 days ago = two fully-passed missed days → hard break
        // even though freezes are banked (a freeze patches only a SINGLE day).
        var focusDays = DaysEndingAt(Today.AddDays(-3), 7);
        var profile = new UserProfile
        {
            FreezesAvailable = 2,
            FreezesEarnedTotal = 3,
            LastFrozenDateUtc = Today.AddDays(-9),
        };

        StreakMath.Evaluate(focusDays, profile, Today);

        Assert.Null(profile.LastFrozenDateUtc);
        Assert.Equal(0, profile.FreezesEarnedTotal);
        Assert.Equal(2, profile.FreezesAvailable); // not consumed on a 2-day gap
    }
}
