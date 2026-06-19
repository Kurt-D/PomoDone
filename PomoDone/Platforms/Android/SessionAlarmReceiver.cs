using Android.App;
using Android.Content;

namespace PomoDone
{
    // Wakes up when the scheduled exact alarm fires (even with the app dead)
    // and posts the end-of-session notification.
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class SessionAlarmReceiver : BroadcastReceiver
    {
        public const string ExtraTitle = "pomodone.title";
        public const string ExtraMessage = "pomodone.message";

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context is null || intent is null)
                return;

            var title = intent.GetStringExtra(ExtraTitle) ?? "Session complete";
            var message = intent.GetStringExtra(ExtraMessage) ?? "";

            // Start the app-owned looping ring FIRST (so it sounds even screen-off /
            // process-just-started), then post the now-silent notification. The ring
            // auto-stops after the max window and can be stopped from the in-app
            // button or by tapping the notification.
            AlarmAudioPlayer.Start(context);
            SessionNotifications.Show(context, title, message);
        }
    }
}
