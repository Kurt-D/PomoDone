using SQLite;

namespace PomoDone.Models;

public enum SessionType
{
    Focus,
    ShortBreak,
    LongBreak
}

public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int? TaskId { get; set; }

    public DateTime StartUtc { get; set; }

    public int DurationMinutes { get; set; }

    public SessionType Type { get; set; }

    public bool Completed { get; set; }

    public int SecondsAway { get; set; }
}
