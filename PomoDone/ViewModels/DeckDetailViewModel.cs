using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Repositories;

namespace PomoDone.ViewModels;

public partial class DeckDetailViewModel : ObservableObject
{
    private readonly DeckRepository _decks;
    private readonly FlashcardRepository _cards;

    // Non-null only while editing an existing card; null means the editor is in
    // "add" (Save & Add Another) mode.
    private Flashcard? _editingCard;

    [ObservableProperty]
    private int _deckId;

    [ObservableProperty]
    private string _deckName = "Deck";

    public ObservableCollection<Flashcard> Cards { get; } = new();

    [ObservableProperty]
    private bool _isEditorVisible;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _editorTitle = "New card";

    [ObservableProperty]
    private string _editorFront = "";

    [ObservableProperty]
    private string _editorBack = "";

    // Derived editor-button visibility/text (avoids an inverse-bool converter).
    public bool IsAddMode => !IsEditMode;
    public string CloseButtonText => IsEditMode ? "Cancel" : "Done";

    // Raised when the editor should put keyboard focus back on the Front field;
    // the page handles the actual Focus() call (view concern).
    public event Action? FocusFrontRequested;

    public DeckDetailViewModel(DeckRepository decks, FlashcardRepository cards)
    {
        _decks = decks;
        _cards = cards;
    }

    public async Task LoadAsync()
    {
        var deck = await _decks.GetByIdAsync(DeckId);
        DeckName = deck?.Name ?? "Deck";
        await ReloadCardsAsync();
    }

    private async Task ReloadCardsAsync()
    {
        var cards = await _cards.GetCardsByDeckAsync(DeckId);
        Cards.Clear();
        foreach (var card in cards)
            Cards.Add(card);
    }

    // "+" opens the editor in add (bulk) mode.
    [RelayCommand]
    private void OpenEditor()
    {
        _editingCard = null;
        IsEditMode = false;
        EditorTitle = "New card";
        EditorFront = "";
        EditorBack = "";
        IsEditorVisible = true;
        FocusFrontRequested?.Invoke();
    }

    [RelayCommand]
    private void CloseEditor() => IsEditorVisible = false;

    // Bulk path: save, clear, keep the editor open with focus back on Front.
    [RelayCommand]
    private async Task SaveAndAddAnotherAsync()
    {
        var front = EditorFront?.Trim();
        if (string.IsNullOrEmpty(front))
        {
            FocusFrontRequested?.Invoke();
            return;
        }

        await _cards.AddCardAsync(new Flashcard
        {
            DeckId = DeckId,
            Front = front,
            Back = EditorBack?.Trim() ?? "",
        });
        await ReloadCardsAsync();

        EditorFront = "";
        EditorBack = "";
        FocusFrontRequested?.Invoke();
    }

    // Opens the editor pre-filled for an existing card (edit mode).
    [RelayCommand]
    private void EditCard(Flashcard? card)
    {
        if (card is null)
            return;

        _editingCard = card;
        IsEditMode = true;
        EditorTitle = "Edit card";
        EditorFront = card.Front;
        EditorBack = card.Back;
        IsEditorVisible = true;
        FocusFrontRequested?.Invoke();
    }

    // Edit path: save the changes and CLOSE (not the bulk path).
    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (_editingCard is null)
            return;

        var front = EditorFront?.Trim();
        if (string.IsNullOrEmpty(front))
        {
            FocusFrontRequested?.Invoke();
            return;
        }

        _editingCard.Front = front;
        _editingCard.Back = EditorBack?.Trim() ?? "";
        await _cards.UpdateCardAsync(_editingCard);
        await ReloadCardsAsync();

        IsEditorVisible = false;
    }

    [RelayCommand]
    private async Task DeleteCardAsync(Flashcard? card)
    {
        if (card is null)
            return;

        await _cards.DeleteCardAsync(card);
        await ReloadCardsAsync();
    }

    // Row "⋮" button → action sheet routing to edit/delete (no row navigation).
    [RelayCommand]
    private async Task ShowCardMenuAsync(Flashcard? card)
    {
        if (card is null)
            return;

        var action = await Shell.Current.DisplayActionSheet(card.Front, "Cancel", null, "Edit", "Delete");
        switch (action)
        {
            case "Edit":
                EditCard(card);
                break;
            case "Delete":
                await DeleteCardAsync(card);
                break;
        }
    }

    partial void OnIsEditModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAddMode));
        OnPropertyChanged(nameof(CloseButtonText));
    }
}
