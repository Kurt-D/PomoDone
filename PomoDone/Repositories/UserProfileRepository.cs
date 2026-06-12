using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class UserProfileRepository
{
    private readonly DatabaseService _database;

    public UserProfileRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<UserProfile?> GetAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<UserProfile>()
            .Where(p => p.Id == UserProfile.SingletonId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(UserProfile profile)
    {
        profile.Id = UserProfile.SingletonId;
        var connection = await _database.GetConnectionAsync();
        return await connection.InsertOrReplaceAsync(profile);
    }
}
