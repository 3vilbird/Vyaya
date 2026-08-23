using Vyaya.Data;
using Vyaya.Models;

namespace Vyaya.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;

    public ExpenseService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Expense> CreateManualExpenseAsync(decimal amount, string category, PaymentMethod method, string? note, DateTime expenseDate)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Category = category,
            PaymentMethod = method,
            Note = note?.Trim(),
            EntryType = ExpenseEntryType.Manual,
            Status = ExpenseStatus.Recorded,
            CreatedAtUtc = DateTime.UtcNow,
            ExpenseDateUtc = expenseDate.ToUniversalTime(),
            CompletedAtUtc = DateTime.UtcNow,
            Currency = "INR"
        };

        await _repository.InsertAsync(expense);
        return expense;
    }

    public async Task<Expense> CreatePendingUpiExpenseAsync(UpiPaymentRequest request, decimal amount, string category, string? userNote)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Category = category,
            PaymentMethod = PaymentMethod.Upi,
            MerchantName = request.PayeeName?.Trim(),
            UpiId = request.PaymentAddress?.Trim(),
            UpiTransactionNote = request.TransactionNote?.Trim(),
            Note = userNote?.Trim(),
            RawUpiPayload = request.RawPayload,
            PaymentReference = request.TransactionReference,
            EntryType = ExpenseEntryType.UpiScan,
            Status = ExpenseStatus.Pending, // Saved BEFORE launching payment app
            CreatedAtUtc = DateTime.UtcNow,
            ExpenseDateUtc = DateTime.UtcNow,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency
        };

        await _repository.InsertAsync(expense);
        return expense;
    }

    public async Task<Expense?> GetExpenseByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<Expense>> GetRecentExpensesAsync(int limit = 10)
    {
        return await _repository.GetRecentAsync(limit);
    }

    public async Task<List<Expense>> GetPendingAttentionExpensesAsync()
    {
        return await _repository.GetPendingOrUnknownAsync();
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<List<Expense>> GetExpensesByMonthAsync(int year, int month)
    {
        return await _repository.GetByMonthAsync(year, month);
    }

    public async Task<List<Expense>> SearchExpensesAsync(string? query, string? category = null, PaymentMethod? method = null, ExpenseStatus? status = null)
    {
        return await _repository.SearchAsync(query, category, method, status);
    }

    public async Task<bool> UpdateExpenseStatusAsync(Guid id, ExpenseStatus status, string? paymentRef = null)
    {
        var expense = await _repository.GetByIdAsync(id);
        if (expense == null)
            return false;

        expense.Status = status;
        if (!string.IsNullOrWhiteSpace(paymentRef))
        {
            expense.PaymentReference = paymentRef;
        }

        if (status == ExpenseStatus.Paid || status == ExpenseStatus.Recorded)
        {
            expense.CompletedAtUtc = DateTime.UtcNow;
        }

        var rows = await _repository.UpdateAsync(expense);
        return rows > 0;
    }

    public async Task<bool> UpdateExpenseDetailsAsync(Expense expense)
    {
        var rows = await _repository.UpdateAsync(expense);
        return rows > 0;
    }

    public async Task<bool> DeleteExpenseAsync(Guid id)
    {
        var rows = await _repository.DeleteAsync(id);
        return rows > 0;
    }
}
