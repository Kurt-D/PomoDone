using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class TimerPage : ContentPage
{
    private readonly TimerViewModel _viewModel;

    public TimerPage(TimerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
