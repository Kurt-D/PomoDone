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
    }

    public TaskItem Model { get; }
    public int Id => Model.Id;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _isDone;

    [ObservableProperty]
    private bool _isActive;

    public string ActiveGlyph => IsActive ? "★" : "☆"; // ★ / ☆

    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(ActiveGlyph));
}
