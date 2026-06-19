using PomoDone.Services;
using Xunit;

namespace PomoDone.Tests;

// Pure unit tests for the focus-purity denominator decision (Bug 1 fix). No DB,
// no MAUI — FocusPurityMath takes the unit scale as a parameter, so minutes-mode
// (60) and DEBUG seconds-mode (1) are both deterministic (mirrors AwayTimeMath).
public class FocusPurityMathTests
{
    private const double Minutes = 60.0; // real minutes-mode + release const
    private const double Seconds = 1.0;  // DEBUG seconds-mode

    // (a) Minutes-mode, 25-min session, fully present → 100%. (Unchanged today.)
    [Fact]
    public void MinutesMode_FullyPresent_Is100()
    {
        var focus = FocusPurityMath.FocusWindowSeconds(25, Minutes); // 1500
        Assert.Equal(1500, focus);
        Assert.Equal(100.0, FocusPurityMath.Percent(focus, 0), 3);
    }

    // (b) Minutes-mode, 25-min session, 5 min (300s) away → 80%. (Unchanged today.)
    [Fact]
    public void MinutesMode_FiveOfTwentyFiveAway_Is80()
    {
        var focus = FocusPurityMath.FocusWindowSeconds(25, Minutes); // 1500
        Assert.Equal(80.0, FocusPurityMath.Percent(focus, 300), 3);
    }

    // (c) Seconds-mode, 25s session, 24s away → ~4% (the bug: was ~100%).
    [Fact]
    public void SecondsMode_TwentyFourOfTwentyFiveAway_IsAbout4()
    {
        var focus = FocusPurityMath.FocusWindowSeconds(25, Seconds); // 25
        Assert.Equal(25, focus);
        Assert.Equal(4.0, FocusPurityMath.Percent(focus, 24), 3);
    }

    // (d) Seconds-mode, 25s session, fully present → 100%.
    [Fact]
    public void SecondsMode_FullyPresent_Is100()
    {
        var focus = FocusPurityMath.FocusWindowSeconds(25, Seconds); // 25
        Assert.Equal(100.0, FocusPurityMath.Percent(focus, 0), 3);
    }

    // Guard: no completed focus time reads as 100% (matches prior == 0 short-circuit).
    [Fact]
    public void ZeroFocusWindow_Is100()
    {
        Assert.Equal(100.0, FocusPurityMath.Percent(0, 0), 3);
    }

    // Away beyond the window is clamped so purity never goes negative.
    [Fact]
    public void AwayExceedingWindow_ClampsToZero()
    {
        var focus = FocusPurityMath.FocusWindowSeconds(25, Seconds); // 25
        Assert.Equal(0.0, FocusPurityMath.Percent(focus, 999), 3);
    }
}
