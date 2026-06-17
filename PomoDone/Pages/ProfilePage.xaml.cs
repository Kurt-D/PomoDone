using PomoDone.Services;
using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;
    private readonly StreakFreezeService _streakFreeze;

    public ProfilePage(ProfileViewModel viewModel, StreakFreezeService streakFreeze)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _streakFreeze = streakFreeze;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Await the freeze pass BEFORE loading so the freeze count and the
        // (possibly freeze-bridged) streak are correct on first paint — not one
        // render behind the fire-and-forget startup pass. This is scoped to the
        // Profile appearing path only; it does not block app launch. The pass is
        // idempotent and its DB writes are serialized on the single connection,
        // so overlapping with the startup pass is harmless. A failure here must
        // never stop the page from loading.
        try
        {
            await _streakFreeze.EvaluateAsync();
        }
        catch
        {
            // ignore — fall through to load whatever state exists
        }

        await _viewModel.LoadAsync();
    }
}
