using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class TimerPage : ContentPage
{
    public TimerPage(TimerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
