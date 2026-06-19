using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class StatsPage : ContentPage
{
    private readonly StatsViewModel _viewModel;

    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Live re-theme: when the app theme changes while this page is visible,
        // rebuild the chart series/axes + heatmap so the C#-resolved colours
        // re-resolve immediately (no restart). Unsubscribed in OnDisappearing.
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;

        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => _viewModel.RefreshTheme();
}
