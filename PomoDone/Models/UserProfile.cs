using SQLite;

namespace PomoDone.Models;

// Exactly one row, always Id = 1. Points/streak/level/badges are derived from
// Session and ReviewLog rows at runtime — never stored here.
public class UserProfile
{
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    public string? DisplayName { get; set; }

    public string? AvatarPath { get; set; }
}
