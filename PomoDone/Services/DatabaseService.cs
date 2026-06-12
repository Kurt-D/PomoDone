using PomoDone.Models;
using SQLite;

namespace PomoDone.Services;

// The ONE SQLiteAsyncConnection for the whole app, registered as a DI singleton.
// All data access goes through the repositories, which call GetConnectionAsync().
public class DatabaseService
{
    // Lazy<Task<T>> with the default ExecutionAndPublication mode guarantees the
    // initialization runs exactly once, even under racing first accesses.
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connection =
        new(InitializeAsync);

    public Task<SQLiteAsyncConnection> GetConnectionAsync() => _connection.Value;

    private static async Task<SQLiteAsyncConnection> InitializeAsync()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pomodone.db3");

        // storeDateTimeAsTicks: true — the project-wide timestamp convention.
        // All DateTime values are written/read as UTC ticks; ViewModels convert
        // to local time for display only.
        var connection = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create,
            storeDateTimeAsTicks: true);

        await connection.EnableWriteAheadLoggingAsync();

        await connection.CreateTableAsync<Session>();
        await connection.CreateTableAsync<TaskItem>();
        await connection.CreateTableAsync<Deck>();
        await connection.CreateTableAsync<Flashcard>();
        await connection.CreateTableAsync<ReviewLog>();
        await connection.CreateTableAsync<UserProfile>();

        return connection;
    }
}
