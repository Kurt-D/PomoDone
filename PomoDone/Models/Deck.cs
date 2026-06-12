using SQLite;

namespace PomoDone.Models;

public class Deck
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}
