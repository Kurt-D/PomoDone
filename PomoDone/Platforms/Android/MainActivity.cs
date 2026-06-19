using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace PomoDone
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SessionNotifications.EnsureChannel(this);
            HandleStopAlarmIntent(Intent); // cold launch from the notification tap
        }

        // SingleTop: when the app is already running, a notification tap arrives
        // here instead of OnCreate. Update the stored Intent and handle it.
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent is not null)
                Intent = intent;
            HandleStopAlarmIntent(intent);
        }

        // Stop path (b): tapping the session-end notification reaches the SAME
        // static handle the receiver and the in-app button use.
        private static void HandleStopAlarmIntent(Intent? intent)
        {
            if (intent?.GetBooleanExtra(SessionNotifications.ExtraStopAlarm, false) == true)
                AlarmAudioPlayer.Stop();
        }
    }
}
