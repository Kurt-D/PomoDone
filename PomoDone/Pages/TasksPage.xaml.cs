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

    // The inline 📌 flips the row's favorite. As a Button it consumes its own
    // tap, so it never reaches the row or the "⋮" menu.
    private void OnToggleFavoriteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: TaskRowViewModel row })
            _viewModel.ToggleFavoriteCommand.Execute(row);
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
