using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class ReviewLogRepository
{
    private readonly DatabaseService _database;

    public ReviewLogRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<ReviewLog>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<ReviewLog>().ToListAsync();
    }

    public async Task<int> SaveAsync(ReviewLog reviewLog)
    {
        var connection = await _database.GetConnectionAsync();
        return reviewLog.Id == 0
            ? await connection.InsertAsync(reviewLog)
            : await connection.UpdateAsync(reviewLog);
    }
}
