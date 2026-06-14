using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomoDone.Models;
using PomoDone.Repositories;
using PomoDone.Services;

namespace PomoDone.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private readonly TaskItemRepository _tasks;
    private readonly ActiveTaskService _activeTask;

    public ObservableCollection<TaskRowViewModel> Tasks { get; } = new();

    [ObservableProperty]
    private string _newTaskTitle = "";

    public TasksViewModel(TaskItemRepository tasks, ActiveTaskService activeTask)
    {
        _tasks = tasks;
        _activeTask = activeTask;
    }

    // Called from TasksPage.OnAppearing so the list (and active marker) is
    // fresh each time the tab is shown.
    public async Task LoadAsync()
    {
        foreach (var row in Tasks)
            row.PropertyChanged -= OnRowPropertyChanged;
        Tasks.Clear();

        var items = await _tasks.GetAllAsync();
        var ordered = items
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.CreatedUtc);

        foreach (var item in ordered)
        {
            var row = new TaskRowViewModel(item)
            {
                IsActive = item.Id == _activeTask.ActiveTaskId,
            };
            row.PropertyChanged += OnRowPropertyChanged;
            Tasks.Add(row);
        }
    }

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        var title = NewTaskTitle?.Trim();
        if (string.IsNullOrEmpty(title))
            return;

        var item = new TaskItem
        {
            Title = title,
            CreatedUtc = DateTime.UtcNow,
            IsDone = false,
        };
        await _tasks.SaveAsync(item);

        NewTaskTitle = "";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(TaskRowViewModel? row)
    {
        if (row is null)
            return;

        if (_activeTask.ActiveTaskId == row.Id)
            _activeTask.Set(null);

        row.PropertyChanged -= OnRowPropertyChanged;
        await _tasks.DeleteAsync(row.Model);
        Tasks.Remove(row);
    }

    // Tapping the star sets (or clears) the timer's active task.
    [RelayCommand]
    private void SetActive(TaskRowViewModel? row)
    {
        if (row is null)
            return;

        var makeActive = _activeTask.ActiveTaskId != row.Id;
        _activeTask.Set(makeActive ? row.Id : null);

        foreach (var r in Tasks)
            r.IsActive = makeActive && r.Id == row.Id;
    }

    public async Task RenameAsync(TaskRowViewModel row, string newTitle)
    {
        var title = newTitle?.Trim();
        if (string.IsNullOrEmpty(title))
            return;

        row.Title = title;
        row.Model.Title = title;
        await _tasks.SaveAsync(row.Model);
    }

    // CheckBox two-way binding flips IsDone, which lands here to persist.
    private async void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TaskRowViewModel row || e.PropertyName != nameof(TaskRowViewModel.IsDone))
            return;

        row.Model.IsDone = row.IsDone;
        row.Model.CompletedUtc = row.IsDone ? DateTime.UtcNow : null;
        await _tasks.SaveAsync(row.Model);

        // A finished task shouldn't stay the timer's active task.
        if (row.IsDone && _activeTask.ActiveTaskId == row.Id)
        {
            _activeTask.Set(null);
            row.IsActive = false;
        }
    }
}
