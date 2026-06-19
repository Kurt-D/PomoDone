using System.ComponentModel;
using PomoDone.Controls;
using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class TimerPage : ContentPage
{
    private readonly TimerViewModel _viewModel;
    private readonly RingDrawable _ring = new();

    public TimerPage(TimerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        RingView.Drawable = _ring;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Redraw the ring whenever the VM's derived Progress changes (it updates
        // on the existing 1-second tick). Display-only — no timer logic here.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Live re-theme: a theme change while the Timer is visible re-resolves
        // the ring's track/arc colours (RingDrawable.Draw pulls the tokens).
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;

        await _viewModel.InitializeAsync();

        _ring.Progress = _viewModel.Progress;
        RingView.Invalidate();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => RingView.Invalidate();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerViewModel.Progress))
        {
            _ring.Progress = _viewModel.Progress;
            RingView.Invalidate();
        }
    }
}
