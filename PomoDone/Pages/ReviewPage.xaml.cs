using PomoDone.ViewModels;

namespace PomoDone.Pages;

public partial class ReviewPage : ContentPage
{
    public ReviewPage(ReviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
