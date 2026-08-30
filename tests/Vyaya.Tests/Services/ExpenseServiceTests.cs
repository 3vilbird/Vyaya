using Vyaya.Models;
using Vyaya.Services;
using Xunit;

namespace Vyaya.Tests.Services;

public class ExpenseServiceTests
{
    private readonly MockExpenseRepository _repository = new();
    private readonly ExpenseService _service;

    public ExpenseServiceTests()
    {
        _service = new ExpenseService(_repository);
    }

    [Fact]
    public async Task CreateManualExpense_SetsRecordedStatusAndManualType()
    {
        var expenseDate = new DateTime(2026, 8, 20);
        var expense = await _service.CreateManualExpenseAsync(
            350m,
            "Food",
            PaymentMethod.Cash,
            "Lunch with friends",
            expenseDate
        );

        Assert.NotNull(expense);
        Assert.Equal(350m, expense.Amount);
        Assert.Equal("Food", expense.Category);
        Assert.Equal(PaymentMethod.Cash, expense.PaymentMethod);
        Assert.Equal("Lunch with friends", expense.Note);
        Assert.Equal(ExpenseStatus.Recorded, expense.Status);
        Assert.Equal(ExpenseEntryType.Manual, expense.EntryType);
        Assert.Equal(expenseDate.ToUniversalTime(), expense.ExpenseDateUtc);
        Assert.Single(_repository.Expenses);
    }

    [Fact]
    public async Task UpdateExpenseDetails_UpdatesProperties()
    {
        var expense = await _service.CreateManualExpenseAsync(200m, "Food", PaymentMethod.Cash, "Initial", DateTime.Today);
        expense.MerchantName = "Corner Cafe";
        expense.Amount = 250m;
        expense.Note = "Updated note";

        var success = await _service.UpdateExpenseDetailsAsync(expense);

        Assert.True(success);
        Assert.Equal("Corner Cafe", expense.MerchantName);
        Assert.Equal(250m, expense.Amount);
        Assert.Equal("Updated note", expense.Note);
    }

    [Fact]
    public async Task DeleteExpense_RemovesFromRepository()
    {
        var expense = await _service.CreateManualExpenseAsync(150m, "Travel", PaymentMethod.Upi, "Auto fare", DateTime.Today);
        Assert.Single(_repository.Expenses);

        var success = await _service.DeleteExpenseAsync(expense.Id);

        Assert.True(success);
        Assert.Empty(_repository.Expenses);
    }

    [Fact]
    public async Task UpdateExpenseStatus_ToPaid_SetsCompletedAtUtc()
    {
        var expense = await _service.CreateManualExpenseAsync(100m, "Other", PaymentMethod.Cash, null, DateTime.Today);
        var success = await _service.UpdateExpenseStatusAsync(expense.Id, ExpenseStatus.Paid, "TXN12345");

        Assert.True(success);
        Assert.Equal(ExpenseStatus.Paid, expense.Status);
        Assert.Equal("TXN12345", expense.PaymentReference);
        Assert.NotNull(expense.CompletedAtUtc);
    }
}
