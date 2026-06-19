using PomoDone.Services;
using Xunit;

namespace PomoDone.Tests;

// Pure unit tests for the session-end ring auto-stop decision. No MAUI, no audio —
// AlarmRingMath takes explicit instants and a max, so each case is deterministic
// (mirrors AwayTimeMath / StreakMath / StudyImpact).
public class AlarmRingMathTests
{
    private static readonly DateTime Start = new(2026, 6, 17, 21, 0, 0, DateTimeKind.Utc);
    private const int Max = AlarmRingMath.DefaultMaxRingSeconds; // 60

    [Fact]
    public void JustStarted_DoesNotStop()
        => Assert.False(AlarmRingMath.ShouldStop(Start, Start, Max));

    [Fact]
    public void WellWithinWindow_DoesNotStop()
        => Assert.False(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(30), Max));

    [Fact]
    public void OneSecondBeforeMax_DoesNotStop()
        => Assert.False(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(59), Max));

    [Fact]
    public void ExactlyAtMax_Stops()
        => Assert.True(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(60), Max));

    [Fact]
    public void PastMax_Stops()
        => Assert.True(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(75), Max));

    [Fact]
    public void RespectsCustomMax()
    {
        Assert.False(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(9), 10));
        Assert.True(AlarmRingMath.ShouldStop(Start, Start.AddSeconds(10), 10));
    }
}
