using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.ViewModels;

// Break-time "review mode" (CLAUDE.md 4.3). NOT spaced repetition: no SM-2, no
// due dates, no scheduling. Cards previously graded "Missed" are simply weighted
// to appear first this run.
public partial class ReviewViewModel : ObservableObject
{
    private readonly FlashcardRepository _cards;
    private readonly ReviewLogRepository _reviews;

    private readonly List<Flashcard> _queue = new();
    private int _index;

    [ObservableProperty]
    private int _deckId;

    [ObservableProperty]
    private string _frontText = "";

    [ObservableProperty]
    private string _backText = "";

    [ObservableProperty]
    private bool _showingBack;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _hasCards;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private int _reviewedCount;

    [ObservableProperty]
    private int _totalCount;

    public bool IsReviewing => HasCards && !IsComplete;
    public bool ShowNoCards => IsLoaded && !HasCards;
    public string CurrentFace => ShowingBack ? BackText : FrontText;
    public string FaceLabel => ShowingBack ? "Back" : "Front";
    public string Progress => TotalCount == 0 ? "" : $"Card {Math.Min(ReviewedCount + 1, TotalCount)} of {TotalCount}";
    public string CompleteSummary => $"Reviewed {ReviewedCount} {(ReviewedCount == 1 ? "card" : "cards")}.";

    public ReviewViewModel(FlashcardRepository cards, ReviewLogRepository reviews)
    {
        _cards = cards;
        _reviews = reviews;
    }

    public async Task LoadAsync()
    {
        _queue.Clear();
        _index = 0;
        ReviewedCount = 0;
        IsComplete = false;
        ShowingBack = false;

        var cards = await _cards.GetCardsByDeckAsync(DeckId);
        if (cards.Count == 0)
        {
            TotalCount = 0;
            HasCards = false;
            IsLoaded = true;
            return;
        }

        _queue.AddRange(await OrderMissedFirstAsync(cards));
        TotalCount = _queue.Count;
        HasCards = true;
        IsLoaded = true;
        ShowCurrent();
    }

    // Shuffle, then float "missed" cards to the front. A card counts as missed
    // when its MOST RECENT review was graded incorrect.
    //
    // Orphaned-log safety: ReviewLog is read standalone (GetAllAsync) and then
    // filtered to FlashcardIds that are in THIS deck's current card set — an
    // in-memory membership test, never a SQL join to Flashcard. Logs whose card
    // was deleted (orphaned, retained for stats) have a FlashcardId that isn't in
    // the set, so they're dropped here and can't affect ordering.
    private async Task<List<Flashcard>> OrderMissedFirstAsync(List<Flashcard> cards)
    {
        var cardIds = cards.Select(c => c.Id).ToHashSet();
        var logs = await _reviews.GetAllAsync();

        var lastByCard = logs
            .Where(l => cardIds.Contains(l.FlashcardId))
            .GroupBy(l => l.FlashcardId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.ReviewedUtc).First());

        bool Missed(Flashcard c) => lastByCard.TryGetValue(c.Id, out var log) && !log.WasCorrect;

        var rng = new Random();
        return cards
            .OrderBy(_ => rng.Next())          // shuffle
            .OrderBy(c => Missed(c) ? 0 : 1)   // stable: missed-first, random within each group
            .ToList();
    }

    private void ShowCurrent()
    {
        var card = _queue[_index];
        FrontText = card.Front;
        BackText = card.Back;
        ShowingBack = false;
    }

    [RelayCommand]
    private void Flip() => ShowingBack = !ShowingBack;

    [RelayCommand]
    private Task GotItAsync() => GradeAsync(true);

    [RelayCommand]
    private Task MissedItAsync() => GradeAsync(false);

    // Each grade writes exactly one standalone ReviewLog row, then advances.
    private async Task GradeAsync(bool wasCorrect)
    {
        if (!IsReviewing)
            return;

        var card = _queue[_index];
        await _reviews.SaveAsync(new ReviewLog
        {
            FlashcardId = card.Id,
            ReviewedUtc = DateTime.UtcNow,
            WasCorrect = wasCorrect,
        });

        // TODO: remove after testing — total ReviewLog count so the VS Output
        // window confirms exactly one row is written per grade.
        var reviewLogCount = (await _reviews.GetAllAsync()).Count;
        System.Diagnostics.Debug.WriteLine($"ReviewLog rows: {reviewLogCount}");

        ReviewedCount++;
        _index++;
        if (_index >= _queue.Count)
            IsComplete = true;
        else
            ShowCurrent();
    }

    [RelayCommand]
    private async Task BackToDeckAsync() => await Shell.Current.GoToAsync("..");

    partial void OnShowingBackChanged(bool value)
    {
        OnPropertyChanged(nameof(CurrentFace));
        OnPropertyChanged(nameof(FaceLabel));
    }

    partial void OnFrontTextChanged(string value) => OnPropertyChanged(nameof(CurrentFace));
    partial void OnBackTextChanged(string value) => OnPropertyChanged(nameof(CurrentFace));

    partial void OnHasCardsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsReviewing));
        OnPropertyChanged(nameof(ShowNoCards));
    }

    partial void OnIsLoadedChanged(bool value) => OnPropertyChanged(nameof(ShowNoCards));
    partial void OnIsCompleteChanged(bool value) => OnPropertyChanged(nameof(IsReviewing));

    partial void OnReviewedCountChanged(int value)
    {
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CompleteSummary));
    }

    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(Progress));
}
