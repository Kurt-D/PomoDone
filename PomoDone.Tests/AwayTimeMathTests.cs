using PomoDone.Services;
using Xunit;

namespace PomoDone.Tests;

// Pure unit tests for the honor-system away-time math (§3.3). No DB, no MAUI —
// AwayTimeMath is dependency-free and every instant is injected, so each case is
// deterministic (mirrors StreakMathTests / StudyImpactTests).
public class AwayTimeMathTests
{
    // A 25-minute Focus window: [Start, Start + 1500s].
    private static readonly DateTime Start = new(2026, 6, 17, 21, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddMinutes(25);

    private static AwayInterval Interval(int leaveOffsetSec, int returnOffsetSec) =>
        new(Start.AddSeconds(leaveOffsetSec), Start.AddSeconds(returnOffsetSec));

    [Fact]
    public void NoAwayEvents_IsZero()
    {
        Assert.Equal(0, AwayTimeMath.TotalAwaySeconds(Array.Empty<AwayInterval>(), Start, End));
    }

    [Fact]
    public void SingleLeaveReturnPair_CountsSeconds()
    {
        // Left 60s in, returned 90s in → 30s away.
        var intervals = new[] { Interval(60, 90) };

        Assert.Equal(30, AwayTimeMath.TotalAwaySeconds(intervals, Start, End));
    }

    [Fact]
    public void LeaveWithoutReturn_CountsUpToCompletionInstant()
    {
        // The user never returned: the caller closes the open interval at the
        // completion instant (here, 600s into the session) → 540s away.
        var completionInstant = Start.AddSeconds(600);
        var open = new AwayInterval(Start.AddSeconds(60), completionInstant);

        Assert.Equal(540, AwayTimeMath.TotalAwaySeconds(new[] { open }, Start, End));
    }

    [Fact]
    public void IntervalStartingBeforeSession_ClampedToStart()
    {
        // Left 30s BEFORE the session, returned 30s in → only the 30s inside count.
        var intervals = new[] { Interval(-30, 30) };

        Assert.Equal(30, AwayTimeMath.TotalAwaySeconds(intervals, Start, End));
    }

    [Fact]
    public void IntervalEndingAfterSession_ClampedToEnd()
    {
        // Left 30s before the end, returned 100s AFTER the end → only 30s count.
        var left = End.AddSeconds(-30);
        var returned = End.AddSeconds(100);

        Assert.Equal(30, AwayTimeMath.TotalAwaySeconds(new[] { new AwayInterval(left, returned) }, Start, End));
    }

    [Fact]
    public void MultiplePairs_AreSummed()
    {
        var intervals = new[]
        {
            Interval(60, 90),    // 30s
            Interval(300, 345),  // 45s
            Interval(600, 605),  // 5s
        };

        Assert.Equal(80, AwayTimeMath.TotalAwaySeconds(intervals, Start, End));
    }

    [Fact]
    public void InvertedOrNonOverlappingInterval_ContributesZero()
    {
        // Entirely after the window, and an inverted pair → both 0.
        var afterWindow = new AwayInterval(End.AddSeconds(10), End.AddSeconds(40));
        var inverted = new AwayInterval(Start.AddSeconds(90), Start.AddSeconds(60));

        Assert.Equal(0, AwayTimeMath.TotalAwaySeconds(new[] { afterWindow, inverted }, Start, End));
    }
}
