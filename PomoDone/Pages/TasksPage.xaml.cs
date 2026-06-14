using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _viewModel;

    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    // Rename prompts live here so the ViewModel stays UI-dialog free.
    private async void OnEditInvoked(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem { CommandParameter: TaskRowViewModel row })
            return;

        var newTitle = await DisplayPromptAsync(
            "Edit task", "Title", initialValue: row.Title, maxLength: 200);

        if (newTitle is not null)
            await _viewModel.RenameAsync(row, newTitle);
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem { CommandParameter: TaskRowViewModel row })
            await _viewModel.DeleteCommand.ExecuteAsync(row);
    }

    private void OnToggleActiveClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: TaskRowViewModel row })
            _viewModel.SetActiveCommand.Execute(row);
    }
}
