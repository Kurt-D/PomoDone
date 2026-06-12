using SQLite;

namespace PomoDone.Models;

// Named TaskItem, never "Task" — would collide with System.Threading.Tasks.Task.
public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public DateTime CreatedUtc { get; set; }

    public bool IsDone { get; set; }

    public DateTime? CompletedUtc { get; set; }
}
