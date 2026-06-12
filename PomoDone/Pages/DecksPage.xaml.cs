using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class DecksPage : ContentPage
{
    public DecksPage(DecksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnDeckDetailClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(DeckDetailPage));

    private async void OnReviewClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ReviewPage));
}
