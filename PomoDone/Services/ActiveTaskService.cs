namespace PomoDone.Services;

// Which TaskItem the timer is currently working on. Deliberately NOT a schema
// column (no "active" field on TaskItem) — a singleton shared between
// TasksViewModel (sets it) and TimerViewModel (reads it). The selected id is
// also written onto the Session row's TaskId when a Focus session starts; that
// FK is the durable per-session record.
//
// The selection itself is persisted in Preferences (one key) so the active
// pick survives process death, matching the wall-clock timer's resilience.
public class ActiveTaskService
{
    private const string PreferenceKey = "active_task_id";

    public ActiveTaskService()
    {
        var stored = Preferences.Get(PreferenceKey, 0);
        ActiveTaskId = stored == 0 ? null : stored;
    }

    public int? ActiveTaskId { get; private set; }

    public void Set(int? taskId)
    {
        ActiveTaskId = taskId;
        if (taskId is null)
            Preferences.Remove(PreferenceKey);
        else
            Preferences.Set(PreferenceKey, taskId.Value);
    }
}
