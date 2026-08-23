using SQLite;
using Vyaya.Models;

namespace Vyaya.Data;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDatabase _database;

    public CategoryRepository(AppDatabase database)
    {
        _database = database;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Category>().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<List<Category>> SearchAsync(string query)
    {
        await _database.InitializeAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllAsync();
        }

        var trimmed = query.Trim().ToLowerInvariant();
        var all = await _database.Connection.Table<Category>().ToListAsync();

        return all
            .Where(c => c.Name.ToLowerInvariant().Contains(trimmed) || 
                       (!string.IsNullOrEmpty(c.Keywords) && c.Keywords.ToLowerInvariant().Contains(trimmed)))
            .OrderByDescending(c => c.Name.ToLowerInvariant().StartsWith(trimmed))
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        await _database.InitializeAsync();
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = name.Trim().ToLowerInvariant();
        var all = await _database.Connection.Table<Category>().ToListAsync();
        return all.FirstOrDefault(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Category>().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> InsertAsync(Category category)
    {
        await _database.InitializeAsync();
        return await _database.Connection.InsertAsync(category);
    }

    public async Task<int> UpdateAsync(Category category)
    {
        await _database.InitializeAsync();
        return await _database.Connection.UpdateAsync(category);
    }
}
