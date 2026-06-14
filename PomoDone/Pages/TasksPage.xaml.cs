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

    // Star toggles the timer's active task (unchanged).
    private void OnToggleActiveClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: TaskRowViewModel row })
            _viewModel.SetActiveCommand.Execute(row);
    }

    // The "⋮" button opens the Edit/Delete action sheet via the ViewModel
    // command, passing the row item as CommandParameter. As a Button it
    // consumes its own tap, so it never reaches the row's other controls.
    private async void OnTaskMenuClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: TaskRowViewModel row })
            await _viewModel.ShowTaskMenuCommand.ExecuteAsync(row);
    }
}
