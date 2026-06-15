using Android.App;
using Android.Content;
using Android.Media;

namespace PomoDone
{
    public static class SessionNotifications
    {
        // A channel's sound/category is immutable once Android creates it, so
        // every change ships under a NEW id and deletes its predecessors.
        // v3 = alarm-category channel so the completion tone rides DND's standard
        // "Alarms" allowance (like a clock alarm).
        public const string ChannelId = "pomodone_sessions_v3";
        private static readonly string[] LegacyChannelIds =
        {
            "pomodone_sessions",    // v1: silent
            "pomodone_sessions_v2", // v2: notification-category sound
        };
        private const int NotificationId = 2001;

        // Idempotent: creating an existing channel is a no-op. Called at app
        // start and again from the receiver, which can fire with the app dead.
        public static void EnsureChannel(Context context)
        {
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            foreach (var legacy in LegacyChannelIds)
                manager.DeleteNotificationChannel(legacy);

            var channel = new NotificationChannel(
                ChannelId, "Session alerts", NotificationImportance.High)
            {
                Description = "Alerts when a focus session or break ends",
            };
            channel.EnableVibration(true);

            // Alarm-category audio + the system default ALARM tone. This makes
            // the completion sound ride DND's default "Alarms" allowance — NOT a
            // forced bypass: under "Total silence" DND, or if the user disallows
            // alarms, it stays silent by their choice. We never call
            // SetBypassDnd or request notification-policy access.
            var audioAttributes = new AudioAttributes.Builder();
            audioAttributes.SetUsage(AudioUsageKind.Alarm);
            audioAttributes.SetContentType(AudioContentType.Sonification);
            channel.SetSound(
                RingtoneManager.GetDefaultUri(RingtoneType.Alarm),
                audioAttributes.Build());

            manager.CreateNotificationChannel(channel);
        }

        public static void Show(Context context, string title, string message)
        {
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            EnsureChannel(context);

            var builder = new Notification.Builder(context, ChannelId);
            builder.SetSmallIcon(Resource.Mipmap.appicon);
            builder.SetContentTitle(title);
            builder.SetContentText(message);
            builder.SetAutoCancel(true);

            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
            if (launchIntent is not null)
            {
                launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                var tapIntent = PendingIntent.GetActivity(
                    context, 0, launchIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                builder.SetContentIntent(tapIntent);
            }

            manager.Notify(NotificationId, builder.Build());
        }
    }
}
