using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class TaskItemRepository
{
    private readonly DatabaseService _database;

    public TaskItemRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<TaskItem>().ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<TaskItem>().Where(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(TaskItem taskItem)
    {
        var connection = await _database.GetConnectionAsync();
        return taskItem.Id == 0
            ? await connection.InsertAsync(taskItem)
            : await connection.UpdateAsync(taskItem);
    }

    public async Task<int> DeleteAsync(TaskItem taskItem)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.DeleteAsync(taskItem);
    }
}
