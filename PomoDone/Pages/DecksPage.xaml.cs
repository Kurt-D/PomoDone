using PomoDone.Models;
using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class DecksPage : ContentPage
{
    private readonly DecksViewModel _viewModel;

    public DecksPage(DecksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    // Tap on the deck's content region (NOT the "⋮" button) navigates.
    private async void OnDeckTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Deck deck)
            await _viewModel.OpenDeckCommand.ExecuteAsync(deck);
    }

    // The "⋮" button opens the action sheet via the ViewModel command, passing
    // the row item as CommandParameter. Being a Button, it consumes its own tap.
    private async void OnDeckMenuClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Deck deck })
            await _viewModel.ShowDeckMenuCommand.ExecuteAsync(deck);
    }
}
