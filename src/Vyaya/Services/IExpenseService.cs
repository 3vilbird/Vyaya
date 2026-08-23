using Vyaya.Models;

namespace Vyaya.Services;

public interface IExpenseService
{
    Task<Expense> CreateManualExpenseAsync(decimal amount, string category, PaymentMethod method, string? note, DateTime expenseDate);
    Task<Expense> CreatePendingUpiExpenseAsync(UpiPaymentRequest request, decimal amount, string category, string? userNote);
    Task<Expense?> GetExpenseByIdAsync(Guid id);
    Task<List<Expense>> GetRecentExpensesAsync(int limit = 10);
    Task<List<Expense>> GetPendingAttentionExpensesAsync();
    Task<List<Expense>> GetAllExpensesAsync();
    Task<List<Expense>> GetExpensesByMonthAsync(int year, int month);
    Task<List<Expense>> SearchExpensesAsync(string? query, string? category = null, PaymentMethod? method = null, ExpenseStatus? status = null);
    Task<bool> UpdateExpenseStatusAsync(Guid id, ExpenseStatus status, string? paymentRef = null);
    Task<bool> UpdateExpenseDetailsAsync(Expense expense);
    Task<bool> DeleteExpenseAsync(Guid id);
}
