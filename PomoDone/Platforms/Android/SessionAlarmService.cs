using Android.App;
using Android.Content;
using PomoDone.Models;
using PomoDone.Services;
using AndroidApp = Android.App.Application;

namespace PomoDone
{
    public class SessionAlarmService : ISessionAlarmService
    {
        // Only one session is ever in progress, so one fixed request code:
        // scheduling replaces any previous PendingIntent, and Cancel matches it.
        private const int RequestCode = 1001;

        public void ScheduleSessionEnd(SessionType type, DateTime endUtc)
        {
            var context = AndroidApp.Context;
            if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
                return;

            // SpecifyKind: rows loaded from SQLite come back Kind=Unspecified,
            // and DateTimeOffset would otherwise treat them as local time.
            var triggerAtMillis = new DateTimeOffset(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc))
                .ToUnixTimeMilliseconds();
            var pending = BuildPendingIntent(context, type);

            // API 31+ lets the user revoke exact-alarm access in settings;
            // degrade to an inexact while-idle alarm rather than crash.
            // USE_EXACT_ALARM (13+) / SCHEDULE_EXACT_ALARM (31-32) in the
            // manifest keep this granted in the normal case.
            if (OperatingSystem.IsAndroidVersionAtLeast(31) && !alarmManager.CanScheduleExactAlarms())
                alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
            else
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
        }

        public void CancelScheduled()
        {
            var context = AndroidApp.Context;
            if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
                return;

            // Extras don't participate in PendingIntent identity, so any type
            // matches the scheduled alarm.
            var pending = BuildPendingIntent(context, SessionType.Focus);
            alarmManager.Cancel(pending);
            pending.Cancel();
        }

        private static PendingIntent BuildPendingIntent(Context context, SessionType type)
        {
            var (title, message) = type switch
            {
                SessionType.Focus => ("Focus session complete!", "Great work — time for a break."),
                SessionType.ShortBreak => ("Break over", "Ready for the next focus session?"),
                _ => ("Long break over", "Ready to get back to it?"),
            };

            var intent = new Intent(context, typeof(SessionAlarmReceiver));
            intent.PutExtra(SessionAlarmReceiver.ExtraTitle, title);
            intent.PutExtra(SessionAlarmReceiver.ExtraMessage, message);

            return PendingIntent.GetBroadcast(
                context, RequestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        }
    }
}
