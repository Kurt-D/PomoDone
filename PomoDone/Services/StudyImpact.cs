using System;
using System.Collections.Generic;
using System.Linq;

namespace PomoDone.Services;

// Derived "Study Impact" analytics — computed purely from ReviewLog rows at
// runtime (no schema change; no stored stat — the §3.5 derive-only philosophy).
// This type is dependency-free (no DB, no MAUI) so it is compiled straight into
// PomoDone.Tests and unit-tested without a phone, exactly like StreakMath.
//
// Inputs carry LOCAL review instants: the ViewModel converts the stored UTC to
// local before calling (§3.4), and `todayLocal` is injected (never DateTime.Now)
// so week-boundary bucketing is deterministic for callers and tests.
public readonly record struct ReviewEntry(int FlashcardId, DateTime ReviewedLocal, bool WasCorrect);

public class StudyImpactResult
{
    // Cards reviewed during breaks in the current local week (Sunday-anchored).
    public int ReviewedThisWeek { get; init; }

    // Whole-percent review accuracy for each week. HasThisWeek/HasLastWeek say
    // whether there were any reviews to average, so the UI can show "—" rather
    // than a misleading 0%.
    public int AccuracyThisWeekPercent { get; init; }
    public int AccuracyLastWeekPercent { get; init; }
    public bool HasThisWeek { get; init; }
    public bool HasLastWeek { get; init; }

    // Headline number: distinct flashcards answered WRONG earlier and RIGHT
    // later — cards the user missed and then came back to get right.
    public int RecoveredCards { get; init; }
}

public static class StudyImpact
{
    // Compute the three demo'd numbers from review history. `todayLocal` is the
    // local "today"; the week starts Sunday 00:00 to match the heatmap grid and
    // GamificationService.CountReviewsThisWeek, so on-screen numbers agree.
    public static StudyImpactResult Compute(IEnumerable<ReviewEntry> entries, DateTime todayLocal)
    {
        var all = entries as IReadOnlyCollection<ReviewEntry> ?? entries.ToList();

        var startOfThisWeek = StartOfWeek(todayLocal);
        var startOfLastWeek = startOfThisWeek.AddDays(-7);
        var startOfNextWeek = startOfThisWeek.AddDays(7);

        var thisWeek = all
            .Where(e => e.ReviewedLocal >= startOfThisWeek && e.ReviewedLocal < startOfNextWeek)
            .ToList();
        var lastWeek = all
            .Where(e => e.ReviewedLocal >= startOfLastWeek && e.ReviewedLocal < startOfThisWeek)
            .ToList();

        return new StudyImpactResult
        {
            ReviewedThisWeek = thisWeek.Count,
            AccuracyThisWeekPercent = AccuracyPercent(thisWeek),
            AccuracyLastWeekPercent = AccuracyPercent(lastWeek),
            HasThisWeek = thisWeek.Count > 0,
            HasLastWeek = lastWeek.Count > 0,
            RecoveredCards = CountRecovered(all),
        };
    }

    // Sunday 00:00 of the local week containing `todayLocal` (Sunday == 0).
    private static DateTime StartOfWeek(DateTime todayLocal)
    {
        var date = todayLocal.Date;
        return date.AddDays(-(int)date.DayOfWeek);
    }

    private static int AccuracyPercent(IReadOnlyCollection<ReviewEntry> window)
    {
        if (window.Count == 0)
            return 0;

        var correct = window.Count(e => e.WasCorrect);
        return (int)Math.Round(100.0 * correct / window.Count, MidpointRounding.AwayFromZero);
    }

    // A card is "recovered" when it was answered incorrectly at some instant and
    // then answered correctly at a strictly LATER instant. Counted across all
    // history, distinct by flashcard. This never joins to Flashcard, so orphaned
    // logs (card later deleted) still count — matching the standalone treatment
    // GamificationService.CountReviewsThisWeek uses.
    private static int CountRecovered(IEnumerable<ReviewEntry> entries)
    {
        var recovered = 0;
        foreach (var group in entries.GroupBy(e => e.FlashcardId))
        {
            var misses = group.Where(e => !e.WasCorrect).Select(e => e.ReviewedLocal).ToList();
            if (misses.Count == 0)
                continue;

            var corrects = group.Where(e => e.WasCorrect).Select(e => e.ReviewedLocal).ToList();
            if (corrects.Count == 0)
                continue;

            if (corrects.Max() > misses.Min())
                recovered++;
        }
        return recovered;
    }
}
