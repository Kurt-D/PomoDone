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

        await _viewModel.InitializeAsync();

        _ring.Progress = _viewModel.Progress;
        RingView.Invalidate();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerViewModel.Progress))
        {
            _ring.Progress = _viewModel.Progress;
            RingView.Invalidate();
        }
    }
}
