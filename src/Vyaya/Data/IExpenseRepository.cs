using Vyaya.Models;

namespace Vyaya.Data;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id);
    Task<List<Expense>> GetAllAsync();
    Task<List<Expense>> GetByMonthAsync(int year, int month);
    Task<List<Expense>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc);
    Task<List<Expense>> GetRecentAsync(int limit = 10);
    Task<List<Expense>> GetPendingOrUnknownAsync();
    Task<int> InsertAsync(Expense expense);
    Task<int> UpdateAsync(Expense expense);
    Task<int> DeleteAsync(Guid id);
    Task<List<Expense>> SearchAsync(string? query, string? category = null, PaymentMethod? method = null, ExpenseStatus? status = null);
    Task<int> GetCountAsync();
    Task<int> ClearAllAsync();
}
