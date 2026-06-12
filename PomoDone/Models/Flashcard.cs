using SQLite;

namespace PomoDone.Models;

public class Flashcard
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int DeckId { get; set; }

    public string Front { get; set; } = "";

    public string Back { get; set; } = "";
}
