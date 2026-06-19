using PomoDone.Helpers;
using PomoDone.Services;

namespace PomoDone
{
    public partial class App : Application
    {
        private readonly StreakFreezeService _streakFreeze;
        private readonly FocusAwayService _away;
        private bool _streakPassRan;

        public App(StreakFreezeService streakFreeze, FocusAwayService away)
        {
            InitializeComponent();
            _streakFreeze = streakFreeze;
            _away = away;

            // Apply the saved dark/light choice before the first page renders
            // (default Dark when unset). Display-only; persisted in Preferences.
            ThemeManager.ApplySavedTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Honor-system focus-purity tracking (§3.3): the window lives for the
            // whole app session, so subscribing here (NOT on a page) catches
            // background/foreground even when the user is on another tab. The
            // window is long-lived and one-way-referenced by the root App, so
            // there is nothing to leak and nothing to unsubscribe.
            window.Deactivated += OnWindowDeactivated;
            window.Activated += OnWindowActivated;

            // Run the streak-freeze pass once per app open (gap-detect → consume
            // → earn → reset). Fire-and-forget so it never blocks launch; the DB
            // init is lazy, so awaiting inside the pass triggers/awaits it. The
            // pass is idempotent, so a second CreateWindow would be harmless, but
            // guard anyway to keep it to one run per process.
            if (!_streakPassRan)
            {
                _streakPassRan = true;
                _ = RunStreakFreezePassAsync();
            }

            return window;
        }

        private async Task RunStreakFreezePassAsync()
        {
            try
            {
                await _streakFreeze.EvaluateAsync();
            }
            catch
            {
                // A freeze-pass failure must never crash or block app launch.
            }
        }

        // App backgrounded: stamp the leave instant if a Focus session is running.
        // Fire-and-forget; away-tracking must never crash or block lifecycle.
        private async void OnWindowDeactivated(object? sender, EventArgs e)
        {
            try { await _away.OnDeactivatedAsync(); }
            catch { }
        }

        // App foregrounded: fold the elapsed time-away onto the running Focus row.
        private async void OnWindowActivated(object? sender, EventArgs e)
        {
            try { await _away.OnActivatedAsync(); }
            catch { }
        }
    }
}