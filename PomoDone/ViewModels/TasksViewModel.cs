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

    // Master list; all rows live here after LoadAsync regardless of tab.
    public ObservableCollection<TaskRowViewModel> Tasks { get; } = new();

    // Filtered subset bound to the CollectionView. Rebuilt whenever
    // SelectedTab changes or the underlying data mutates (done-toggle,
    // favorite-toggle, delete).
    public ObservableCollection<TaskRowViewModel> FilteredTasks { get; } = new();

    [ObservableProperty]
    private string _newTaskTitle = "";

    // 0 = Active (IsDone=false)  |  1 = Completed (IsDone=true)
    [ObservableProperty]
    private int _selectedTab = 0;

    public bool IsActiveTabSelected => SelectedTab == 0;
    public bool IsCompletedTabSelected => SelectedTab == 1;
    public string EmptyTasksMessage => SelectedTab == 0 ? "No active tasks." : "No completed tasks.";

    public TasksViewModel(TaskItemRepository tasks, ActiveTaskService activeTask)
    {
        _tasks = tasks;
        _activeTask = activeTask;
    }

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsActiveTabSelected));
        OnPropertyChanged(nameof(IsCompletedTabSelected));
        OnPropertyChanged(nameof(EmptyTasksMessage));
        RebuildFilteredTasks();
    }

    [RelayCommand] private void SelectActiveTab() => SelectedTab = 0;
    [RelayCommand] private void SelectCompletedTab() => SelectedTab = 1;

    private void RebuildFilteredTasks()
    {
        FilteredTasks.Clear();
        var source = SelectedTab == 0
            ? Tasks
                .Where(t => !t.IsDone)
                .OrderByDescending(t => t.IsFavorite)
                .ThenByDescending(t => t.Model.CreatedUtc)
            : Tasks
                .Where(t => t.IsDone)
                .OrderByDescending(t => t.Model.CreatedUtc);
        foreach (var row in source)
            FilteredTasks.Add(row);
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
            .ThenByDescending(t => t.IsFavorite)
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

        RebuildFilteredTasks();
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
        SelectedTab = 0; // jump to Active so the new task is visible
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
        RebuildFilteredTasks();
    }

    // Sets (or clears) the timer's active task. Invoked from the "⋮" menu.
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

    // Inline ★ toggle. Flips IsFavorite and persists, then re-sorts the row IN
    // PLACE — no Clear()/reload, so the collection never empties (no empty-state
    // flash) and there's no DB round-trip. Isolated from row-tap / "⋮".
    [RelayCommand]
    private async Task ToggleFavoriteAsync(TaskRowViewModel? row)
    {
        if (row is null)
            return;

        row.IsFavorite = !row.IsFavorite;
        row.Model.IsFavorite = row.IsFavorite;
        await _tasks.SaveAsync(row.Model);

        var oldIndex = Tasks.IndexOf(row);
        if (oldIndex >= 0)
        {
            // Target position under the same sort LoadAsync uses (IsDone asc,
            // IsFavorite desc, CreatedUtc desc): count the rows that sort ahead.
            var newIndex = Tasks.Count(other => !ReferenceEquals(other, row) && SortsBefore(other, row));
            if (newIndex != oldIndex)
                Tasks.Move(oldIndex, newIndex);
        }

        RebuildFilteredTasks();
    }

    // Mirrors the LoadAsync ordering: undone first, then favorites, then newest.
    private static bool SortsBefore(TaskRowViewModel a, TaskRowViewModel b)
    {
        if (a.Model.IsDone != b.Model.IsDone)
            return !a.Model.IsDone;          // not-done before done
        if (a.Model.IsFavorite != b.Model.IsFavorite)
            return a.Model.IsFavorite;       // favorite before not
        return a.Model.CreatedUtc > b.Model.CreatedUtc; // newer before older
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

    // Triggered by the row's "⋮" button. Set/Clear Active, Edit, and Delete
    // live here; mark-done (CheckBox) and favorite (★) stay inline.
    [RelayCommand]
    private async Task ShowTaskMenuAsync(TaskRowViewModel? row)
    {
        if (row is null)
            return;

        var activeLabel = row.IsActive ? "Clear Active" : "Set Active";
        var action = await Shell.Current.DisplayActionSheet(
            row.Title, "Cancel", null, activeLabel, "Edit", "Delete");
        switch (action)
        {
            case "Set Active":
            case "Clear Active":
                SetActive(row);
                break;
            case "Edit":
                var newTitle = await Shell.Current.DisplayPromptAsync(
                    "Edit task", "Title", initialValue: row.Title, maxLength: 200);
                if (newTitle is not null)
                    await RenameAsync(row, newTitle);
                break;
            case "Delete":
                await DeleteAsync(row);
                break;
        }
    }

    // CheckBox two-way binding flips IsDone, which lands here to persist and
    // move the row between Active / Completed filtered views.
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

        // Move the row between Active / Completed tabs.
        RebuildFilteredTasks();
    }
}
