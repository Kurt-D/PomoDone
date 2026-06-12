using SQLite;

namespace PomoDone.Models;

public class ReviewLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int FlashcardId { get; set; }

    public DateTime ReviewedUtc { get; set; }

    public bool WasCorrect { get; set; }
}
