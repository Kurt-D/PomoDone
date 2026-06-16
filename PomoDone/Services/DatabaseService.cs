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

        await SeedSampleDataAsync(connection);

        return connection;
    }

    // First-launch seed of the sample deck. Runs inside the Lazy<Task> once-
    // guard (so it cannot race or fire twice) using the connection just created.
    // It must use that local connection directly, NOT the repositories: a repo
    // call would re-enter GetConnectionAsync()/_connection.Value while the Lazy
    // is still initializing and throw. Guard: seed only when zero decks exist,
    // which means it can never double-seed (even after force-close/relaunch, and
    // it stays off once the user has any deck of their own).
    private static async Task SeedSampleDataAsync(SQLiteAsyncConnection connection)
    {
        var deckCount = await connection.Table<Deck>().CountAsync();
        if (deckCount > 0)
            return;

        var deck = new Deck { Name = SampleData.DeckName };
        await connection.InsertAsync(deck); // autoincrement populates deck.Id
        await connection.InsertAllAsync(SampleData.BuildCards(deck.Id));
    }
}
