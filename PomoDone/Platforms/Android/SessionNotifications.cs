using Android.App;
using Android.Content;

namespace PomoDone
{
    public static class SessionNotifications
    {
        public const string ChannelId = "pomodone_sessions";
        private const int NotificationId = 2001;

        // Idempotent: creating an existing channel is a no-op. Called at app
        // start and again from the receiver, which can fire with the app dead.
        public static void EnsureChannel(Context context)
        {
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            var channel = new NotificationChannel(
                ChannelId, "Session alerts", NotificationImportance.High)
            {
                Description = "Alerts when a focus session or break ends",
            };
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
