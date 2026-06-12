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

    public async Task<List<Deck>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Deck>().ToListAsync();
    }

    public async Task<Deck?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Deck>().Where(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(Deck deck)
    {
        var connection = await _database.GetConnectionAsync();
        return deck.Id == 0
            ? await connection.InsertAsync(deck)
            : await connection.UpdateAsync(deck);
    }

    public async Task<int> DeleteAsync(Deck deck)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.DeleteAsync(deck);
    }
}
