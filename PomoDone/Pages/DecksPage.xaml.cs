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

    // Tapping a deck navigates to its detail page. Selection is cleared so the
    // same deck can be tapped again after returning.
    private async void OnDeckSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView list && e.CurrentSelection.FirstOrDefault() is Deck deck)
        {
            list.SelectedItem = null;
            await _viewModel.OpenDeckCommand.ExecuteAsync(deck);
        }
    }

    // Swipe actions route to the ViewModel's commands (keeps logic in the VM
    // while keeping the item bindings compiled).
    private async void OnRenameInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem { CommandParameter: Deck deck })
            await _viewModel.RenameDeckCommand.ExecuteAsync(deck);
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem { CommandParameter: Deck deck })
            await _viewModel.DeleteDeckCommand.ExecuteAsync(deck);
    }
}
