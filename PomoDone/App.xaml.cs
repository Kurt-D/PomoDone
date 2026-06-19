using PomoDone.Helpers;
using PomoDone.Services;

namespace PomoDone
{
    public partial class App : Application
    {
        private readonly StreakFreezeService _streakFreeze;
        private bool _streakPassRan;

        public App(StreakFreezeService streakFreeze)
        {
            InitializeComponent();
            _streakFreeze = streakFreeze;

            // Apply the saved dark/light choice before the first page renders
            // (default Dark when unset). Display-only; persisted in Preferences.
            ThemeManager.ApplySavedTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

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
    }
}