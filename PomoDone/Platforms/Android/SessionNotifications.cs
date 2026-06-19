using Android.App;
using Android.Content;

namespace PomoDone
{
    public static class SessionNotifications
    {
        // Extra flag on the tap intent telling MainActivity to stop the ring when
        // the user opens the app from this notification.
        public const string ExtraStopAlarm = "pomodone.stop_alarm";

        // A channel's sound/category is immutable once Android creates it, so
        // every change ships under a NEW id and deletes its predecessors.
        // v4 = SILENT channel: the app now owns the looping ring (AlarmAudioPlayer)
        // so it can be stopped from inside the app, which a channel sound (a system
        // one-shot) never allowed. The channel carries no sound at all.
        public const string ChannelId = "pomodone_sessions_v4";
        private static readonly string[] LegacyChannelIds =
        {
            "pomodone_sessions",    // v1: silent
            "pomodone_sessions_v2", // v2: notification-category sound
            "pomodone_sessions_v3", // v3: alarm-category channel sound (now app-owned)
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

            // SILENT channel: no SetSound. The looping alarm tone is played and
            // stopped by the app itself (AlarmAudioPlayer), so the channel must not
            // also fire a one-shot the app can't control.
            channel.SetSound(null, null);

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
                // Tapping the notification reopens the app AND stops the ring
                // (preserving the old tap-to-silence behaviour now that the app
                // owns the audio). MainActivity reads this extra and calls Stop().
                launchIntent.PutExtra(ExtraStopAlarm, true);
                var tapIntent = PendingIntent.GetActivity(
                    context, 0, launchIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                builder.SetContentIntent(tapIntent);
            }

            manager.Notify(NotificationId, builder.Build());
        }
    }
}
