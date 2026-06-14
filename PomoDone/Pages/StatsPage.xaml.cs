using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class StatsPage : ContentPage
{
    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
