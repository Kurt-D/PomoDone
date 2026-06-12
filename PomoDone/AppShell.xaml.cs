using PomoDone.Pages;

namespace PomoDone
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Sub-pages reached via Shell.Current.GoToAsync; tab pages are
            // declared in AppShell.xaml and need no registration here.
            Routing.RegisterRoute(nameof(DeckDetailPage), typeof(DeckDetailPage));
            Routing.RegisterRoute(nameof(ReviewPage), typeof(ReviewPage));
        }
    }
}
