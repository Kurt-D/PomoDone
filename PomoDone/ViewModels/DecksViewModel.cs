using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Pages;
using PomoDone.Repositories;

namespace PomoDone.ViewModels;

public partial class DecksViewModel : ObservableObject
{
    private readonly DeckRepository _repository;

    [ObservableProperty]
    private ObservableCollection<Deck> _decks = new();

    [ObservableProperty]
    private string _newDeckName = "";

    public DecksViewModel(DeckRepository repository)
    {
        _repository = repository;
    }

    // Called from DecksPage.OnAppearing so the list refreshes each time the
    // tab is shown (e.g. after returning from a deck).
    public async Task LoadAsync()
    {
        var decks = await _repository.GetDecksAsync();
        Decks = new ObservableCollection<Deck>(decks);
    }

    [RelayCommand]
    private async Task AddDeckAsync()
    {
        var name = NewDeckName?.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        await _repository.AddDeckAsync(new Deck { Name = name });
        NewDeckName = "";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RenameDeckAsync(Deck? deck)
    {
        if (deck is null)
            return;

        var newName = await Shell.Current.DisplayPromptAsync(
            "Rename deck", "Deck name", initialValue: deck.Name, maxLength: 100);
        if (string.IsNullOrWhiteSpace(newName))
            return;

        deck.Name = newName.Trim();
        await _repository.UpdateDeckAsync(deck);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteDeckAsync(Deck? deck)
    {
        if (deck is null)
            return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Delete deck",
            "Are you sure? This will delete all cards in this deck. Your review history and stats will be kept.",
            "Delete",
            "Cancel");
        if (!confirmed)
            return;

        await _repository.DeleteDeckAsync(deck);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenDeckAsync(Deck? deck)
    {
        if (deck is null)
            return;

        // Shell route registered centrally in AppShell.xaml.cs; pass the deck
        // Id as a query param for DeckDetailPage to load (next step).
        await Shell.Current.GoToAsync($"{nameof(DeckDetailPage)}?deckId={deck.Id}");
    }

    // Triggered by the row's "⋮" button. An action sheet replaces the old swipe
    // gesture (which competed with the row tap); it just routes to the existing
    // rename/delete flows — delete semantics are unchanged.
    [RelayCommand]
    private async Task ShowDeckMenuAsync(Deck? deck)
    {
        if (deck is null)
            return;

        var action = await Shell.Current.DisplayActionSheet(deck.Name, "Cancel", null, "Rename", "Delete");
        switch (action)
        {
            case "Rename":
                await RenameDeckAsync(deck);
                break;
            case "Delete":
                await DeleteDeckAsync(deck);
                break;
        }
    }
}
