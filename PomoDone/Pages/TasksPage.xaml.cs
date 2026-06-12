using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class TasksPage : ContentPage
{
    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
