using PomoDone.Models;
using PomoDone.Services;

namespace PomoDone.Repositories;

public class DeckRepository
{
    private readonly DatabaseService _database;

    public DeckRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<Deck>> GetDecksAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Deck>().ToListAsync();
    }

    public async Task<Deck?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Deck>().Where(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> AddDeckAsync(Deck deck)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.InsertAsync(deck);
    }

    public async Task<int> UpdateDeckAsync(Deck deck)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.UpdateAsync(deck);
    }

    // Deletes the deck and its cards, but DELIBERATELY retains the ReviewLog
    // rows tied to those cards. A user freeing up space by deleting a deck
    // shouldn't lose their consistency stats or earned review points. The
    // ReviewLog rows become orphaned (FlashcardId points at a deleted card) —
    // intentional: they exist only to be counted by the review stat and the
    // point bonus, never joined back to a card.
    public async Task DeleteDeckAsync(Deck deck)
    {
        var connection = await _database.GetConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM Flashcard WHERE DeckId = ?", deck.Id);
        await connection.DeleteAsync(deck);
    }
}
