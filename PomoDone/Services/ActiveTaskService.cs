namespace PomoDone.Services;

// Which TaskItem the timer is currently working on. This is ephemeral UI
// state, deliberately NOT in the schema (no "active" column) — a singleton
// shared between TasksViewModel (sets it) and TimerViewModel (reads it).
// The selected id is persisted onto the Session row's TaskId when a Focus
// session starts; that FK is the durable record.
public class ActiveTaskService
{
    public int? ActiveTaskId { get; private set; }

    public void Set(int? taskId) => ActiveTaskId = taskId;
}
