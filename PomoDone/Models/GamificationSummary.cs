namespace PomoDone.Models;

// Snapshot of all derived gamification values for a single render. Produced
// by GamificationService from Session + ReviewLog rows; holds no identity of
// its own and is never persisted.
public class GamificationSummary
{
    public int Points { get; init; }
    public int Streak { get; init; }
    public int Level { get; init; }
    public int DaysActive { get; init; }
    public int CompletedFocusSessions { get; init; }
    public int ReviewCount { get; init; }
    public double FocusPurityPercent { get; init; }
    public IReadOnlyList<Badge> Badges { get; init; } = Array.Empty<Badge>();
}
