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

    public async Task<List<Flashcard>> GetCardsByDeckAsync(int deckId)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Flashcard>().Where(f => f.DeckId == deckId).ToListAsync();
    }

    public async Task<int> AddCardAsync(Flashcard card)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.InsertAsync(card);
    }

    public async Task<int> UpdateCardAsync(Flashcard card)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.UpdateAsync(card);
    }

    // Single-card delete. No deck-level cascade and no ReviewLog involvement —
    // deck deletion (which retains ReviewLog) lives in DeckRepository.
    public async Task<int> DeleteCardAsync(Flashcard card)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.DeleteAsync(card);
    }
}
