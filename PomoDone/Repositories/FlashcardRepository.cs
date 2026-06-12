using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class FlashcardRepository
{
    private readonly DatabaseService _database;

    public FlashcardRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Flashcard>> GetByDeckAsync(int deckId)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Flashcard>().Where(f => f.DeckId == deckId).ToListAsync();
    }

    public async Task<Flashcard?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Flashcard>().Where(f => f.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(Flashcard flashcard)
    {
        var connection = await _database.GetConnectionAsync();
        return flashcard.Id == 0
            ? await connection.InsertAsync(flashcard)
            : await connection.UpdateAsync(flashcard);
    }

    public async Task<int> DeleteAsync(Flashcard flashcard)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.DeleteAsync(flashcard);
    }
}
