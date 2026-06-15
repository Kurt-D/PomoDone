using PomoDone.Models;
using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class DeckDetailPage : ContentPage, IQueryAttributable
{
    private readonly DeckDetailViewModel _viewModel;

    public DeckDetailPage(DeckDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // The ViewModel asks the view to focus the Front field (a view concern).
        _viewModel.FocusFrontRequested += OnFocusFrontRequested;
    }

    // Shell delivers the deckId query param here before the page appears.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("deckId", out var raw) && int.TryParse(raw?.ToString(), out var deckId))
            _viewModel.DeckId = deckId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private void OnFocusFrontRequested()
    {
        // Defer so the editor is laid out/visible before focusing.
        MainThread.BeginInvokeOnMainThread(() => FrontEditor.Focus());
    }

    private async void OnCardMenuClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Flashcard card })
            await _viewModel.ShowCardMenuCommand.ExecuteAsync(card);
    }
}
