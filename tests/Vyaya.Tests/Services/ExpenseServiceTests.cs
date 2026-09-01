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

    [Fact]
    public async Task CreateManualExpense_PreservesSpecificTime_EffectiveDateLocalMatches()
    {
        // 9:30 PM (21:30) on Sept 1, 2026
        var localDateTime = new DateTime(2026, 9, 1, 21, 30, 0, DateTimeKind.Local);

        var expense = await _service.CreateManualExpenseAsync(
            500m,
            "Food",
            PaymentMethod.Cash,
            "Dinner",
            localDateTime
        );

        Assert.Equal(21, expense.EffectiveDateLocal.Hour);
        Assert.Equal(30, expense.EffectiveDateLocal.Minute);
        Assert.Equal(1, expense.EffectiveDateLocal.Day);
        Assert.Equal(9, expense.EffectiveDateLocal.Month);
        Assert.Equal(2026, expense.EffectiveDateLocal.Year);

        Assert.Equal("09:30 PM", expense.FormattedTime);
        Assert.Contains("09:30 PM", expense.DisplaySubtitle);
        Assert.Contains("01 Sep 2026, 09:30 PM", expense.FormattedDateTime);
    }
}
