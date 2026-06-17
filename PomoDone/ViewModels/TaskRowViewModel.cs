using CommunityToolkit.Mvvm.ComponentModel;
using PomoDone.Models;

namespace PomoDone.ViewModels;

// Observable wrapper around a TaskItem so the list can react to done-toggles
// and active highlighting without making the persisted model observable.
// The underlying TaskItem stays a plain POCO for sqlite-net.
public partial class TaskRowViewModel : ObservableObject
{
    public TaskRowViewModel(TaskItem model)
    {
        Model = model;
        _title = model.Title;
        _isDone = model.IsDone;
        _isFavorite = model.IsFavorite;
    }

    public TaskItem Model { get; }
    public int Id => Model.Id;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _isDone;

    // Active = the timer's current task. No inline glyph any more (toggled from
    // the "⋮" menu); the flag still drives that menu's label and timer sync.
    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isFavorite;

    // The 📌 is always shown; favorited rows render it solid, others dimmed.
    public double FavoriteOpacity => IsFavorite ? 1.0 : 0.3;

    partial void OnIsFavoriteChanged(bool value) => OnPropertyChanged(nameof(FavoriteOpacity));
}
