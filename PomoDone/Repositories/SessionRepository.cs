using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class SessionRepository
{
    private readonly DatabaseService _database;

    public SessionRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Session>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Session>().ToListAsync();
    }

    // At most one row is ever in progress: completing a session sets
    // Completed = true and cancelling deletes the row.
    public async Task<Session?> GetInProgressAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Session>()
            .Where(s => s.Completed == false)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Session?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Session>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(Session session)
    {
        var connection = await _database.GetConnectionAsync();
        return session.Id == 0
            ? await connection.InsertAsync(session)
            : await connection.UpdateAsync(session);
    }

    public async Task<int> DeleteAsync(Session session)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.DeleteAsync(session);
    }

    // Bulk insert for the demo-data seeder (one statement, far faster than
    // looping SaveAsync over ~6 weeks of rows).
    public async Task<int> InsertAllAsync(IEnumerable<Session> sessions)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.InsertAllAsync(sessions);
    }
}
