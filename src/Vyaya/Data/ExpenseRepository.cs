using SQLite;
using Vyaya.Models;

namespace Vyaya.Data;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDatabase _database;

    public ExpenseRepository(AppDatabase database)
    {
        _database = database;
    }

    public async Task<Expense?> GetByIdAsync(Guid id)
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Expense>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Expense>> GetAllAsync()
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Expense>()
            .OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<List<Expense>> GetByMonthAsync(int year, int month)
    {
        await _database.InitializeAsync();
        var all = await _database.Connection.Table<Expense>().ToListAsync();
        return all
            .Where(e =>
            {
                var localDate = e.EffectiveDateLocal;
                return localDate.Year == year && localDate.Month == month;
            })
            .OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc)
            .ToList();
    }

    public async Task<List<Expense>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc)
    {
        await _database.InitializeAsync();
        var all = await _database.Connection.Table<Expense>().ToListAsync();
        return all
            .Where(e =>
            {
                var date = (e.ExpenseDateUtc ?? e.CreatedAtUtc).ToUniversalTime();
                return date >= startUtc && date <= endUtc;
            })
            .OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc)
            .ToList();
    }

    public async Task<List<Expense>> GetRecentAsync(int limit = 10)
    {
        await _database.InitializeAsync();
        var all = await _database.Connection.Table<Expense>().ToListAsync();
        return all
            .OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc)
            .Take(limit)
            .ToList();
    }

    public async Task<List<Expense>> GetPendingOrUnknownAsync()
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Expense>()
            .Where(e => e.EntryType == ExpenseEntryType.UpiScan && 
                       (e.Status == ExpenseStatus.Pending || e.Status == ExpenseStatus.Unknown))
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<int> InsertAsync(Expense expense)
    {
        await _database.InitializeAsync();
        return await _database.Connection.InsertAsync(expense);
    }

    public async Task<int> UpdateAsync(Expense expense)
    {
        await _database.InitializeAsync();
        return await _database.Connection.UpdateAsync(expense);
    }

    public async Task<int> DeleteAsync(Guid id)
    {
        await _database.InitializeAsync();
        return await _database.Connection.DeleteAsync<Expense>(id);
    }

    public async Task<List<Expense>> SearchAsync(string? query, string? category = null, PaymentMethod? method = null, ExpenseStatus? status = null)
    {
        await _database.InitializeAsync();
        var all = await _database.Connection.Table<Expense>().ToListAsync();

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            filtered = filtered.Where(e =>
                (!string.IsNullOrEmpty(e.MerchantName) && e.MerchantName.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(e.Category) && e.Category.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(e.Note) && e.Note.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(e.UpiId) && e.UpiId.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(e.UpiTransactionNote) && e.UpiTransactionNote.ToLowerInvariant().Contains(q))
            );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (method.HasValue)
        {
            filtered = filtered.Where(e => e.PaymentMethod == method.Value);
        }

        if (status.HasValue)
        {
            filtered = filtered.Where(e => e.Status == status.Value);
        }

        return filtered
            .OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc)
            .ToList();
    }

    public async Task<int> GetCountAsync()
    {
        await _database.InitializeAsync();
        return await _database.Connection.Table<Expense>().CountAsync();
    }

    public async Task<int> ClearAllAsync()
    {
        await _database.InitializeAsync();
        return await _database.Connection.DeleteAllAsync<Expense>();
    }
}
