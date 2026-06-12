using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class DeckDetailPage : ContentPage
{
    public DeckDetailPage(DeckDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
