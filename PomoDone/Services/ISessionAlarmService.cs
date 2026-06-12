using PomoDone.Models;

namespace PomoDone.Services;

// Implemented in Platforms/Android with AlarmManager exact alarms. The alarm
// only delivers the end-of-session notification — it survives Doze,
// swipe-away, and process death. Completion writes stay in TimerViewModel,
// where the wall clock is the source of truth.
public interface ISessionAlarmService
{
    void ScheduleSessionEnd(SessionType type, DateTime endUtc);

    void CancelScheduled();
}
