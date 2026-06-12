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
            SessionNotifications.Show(context, title, message);
        }
    }
}
