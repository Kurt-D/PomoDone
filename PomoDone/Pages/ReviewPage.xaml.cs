using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class ReviewPage : ContentPage, IQueryAttributable
{
    private readonly ReviewViewModel _viewModel;

    public ReviewPage(ReviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
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
}
