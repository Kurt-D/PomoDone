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

    // User-pinned favorite. Persisted task state (not gamification), so it lives
    // on the row — sqlite-net's CreateTableAsync adds this column to existing
    // tables automatically, defaulting old rows to false.
    public bool IsFavorite { get; set; }
}
