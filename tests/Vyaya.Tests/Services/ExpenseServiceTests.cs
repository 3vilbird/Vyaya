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
    public async Task CreatePendingUpiExpense_SetsPendingStatusAndUpiScanType()
    {
        var request = new UpiPaymentRequest
        {
            PaymentAddress = "vendor@upi",
            PayeeName = "ABC Store",
            Amount = 450m,
            Currency = "INR",
            TransactionNote = "Groceries order"
        };

        var expense = await _service.CreatePendingUpiExpenseAsync(
            request,
            450m,
            "Groceries",
            "My custom note"
        );

        Assert.NotNull(expense);
        Assert.Equal(450m, expense.Amount);
        Assert.Equal("ABC Store", expense.MerchantName);
        Assert.Equal("vendor@upi", expense.UpiId);
        Assert.Equal("Groceries order", expense.UpiTransactionNote);
        Assert.Equal("My custom note", expense.Note);
        Assert.Equal(ExpenseStatus.Pending, expense.Status);
        Assert.Equal(ExpenseEntryType.UpiScan, expense.EntryType);
        Assert.Single(_repository.Expenses);
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
