using Vyaya.Data;
using Vyaya.Models;
using Vyaya.Services;
using Xunit;

namespace Vyaya.Tests.Services;

public class MockExpenseRepository : IExpenseRepository
{
    public List<Expense> Expenses { get; } = new();

    public Task<Expense?> GetByIdAsync(Guid id) => Task.FromResult(Expenses.FirstOrDefault(e => e.Id == id));
    public Task<List<Expense>> GetAllAsync() => Task.FromResult(Expenses.ToList());
    public Task<List<Expense>> GetByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        return Task.FromResult(Expenses.Where(e =>
        {
            var d = (e.ExpenseDateUtc ?? e.CreatedAtUtc).ToUniversalTime();
            return d >= start && d <= end;
        }).ToList());
    }
    public Task<List<Expense>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc) =>
        Task.FromResult(Expenses.Where(e => (e.ExpenseDateUtc ?? e.CreatedAtUtc) >= startUtc && (e.ExpenseDateUtc ?? e.CreatedAtUtc) <= endUtc).ToList());
    public Task<List<Expense>> GetRecentAsync(int limit = 10) =>
        Task.FromResult(Expenses.OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc).Take(limit).ToList());
    public Task<List<Expense>> GetPendingOrUnknownAsync() =>
        Task.FromResult(Expenses.Where(e => e.Status == ExpenseStatus.Pending || e.Status == ExpenseStatus.Unknown).ToList());
    public Task<int> InsertAsync(Expense expense) { Expenses.Add(expense); return Task.FromResult(1); }
    public Task<int> UpdateAsync(Expense expense) { return Task.FromResult(1); }
    public Task<int> DeleteAsync(Guid id) { Expenses.RemoveAll(e => e.Id == id); return Task.FromResult(1); }
    public Task<List<Expense>> SearchAsync(string? query, string? category = null, PaymentMethod? method = null, ExpenseStatus? status = null) =>
        Task.FromResult(Expenses.ToList());
    public Task<int> GetCountAsync() => Task.FromResult(Expenses.Count);
    public Task<int> ClearAllAsync() { Expenses.Clear(); return Task.FromResult(1); }
}

public class MockCategoryRepository : ICategoryRepository
{
    public List<Category> Categories { get; } = new()
    {
        new() { Name = "Food", Icon = "🍔", ColorHex = "#FF9500" },
        new() { Name = "Travel", Icon = "✈️", ColorHex = "#007AFF" },
        new() { Name = "Groceries", Icon = "🛒", ColorHex = "#30B0C7" },
        new() { Name = "Shopping", Icon = "🛍️", ColorHex = "#AF52DE" },
        new() { Name = "Bills", Icon = "🧾", ColorHex = "#5856D6" },
        new() { Name = "Other", Icon = "📦", ColorHex = "#8E8E93" }
    };

    public Task<List<Category>> GetAllAsync() => Task.FromResult(Categories.ToList());
    public Task<List<Category>> SearchAsync(string query) => Task.FromResult(Categories.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());
    public Task<Category?> GetByNameAsync(string name) => Task.FromResult(Categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    public Task<Category?> GetByIdAsync(int id) => Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));
    public Task<int> InsertAsync(Category category) { Categories.Add(category); return Task.FromResult(1); }
    public Task<int> UpdateAsync(Category category) => Task.FromResult(1);
}

public class ExpenseReportServiceTests
{
    private readonly MockExpenseRepository _expenseRepo = new();
    private readonly MockCategoryRepository _catRepo = new();
    private readonly ExpenseReportService _reportService;

    public ExpenseReportServiceTests()
    {
        _reportService = new ExpenseReportService(_expenseRepo, _catRepo);
    }

    [Fact]
    public async Task GetMonthlySummary_IncludesPaidAndRecorded_ExcludesPendingFailedCancelledUnknown()
    {
        var targetDate = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

        _expenseRepo.Expenses.AddRange(new[]
        {
            new Expense { Amount = 500m, Category = "Food", Status = ExpenseStatus.Paid, ExpenseDateUtc = targetDate, PaymentMethod = PaymentMethod.Upi },
            new Expense { Amount = 150m, Category = "Travel", Status = ExpenseStatus.Recorded, ExpenseDateUtc = targetDate, PaymentMethod = PaymentMethod.Cash },
            new Expense { Amount = 1000m, Category = "Shopping", Status = ExpenseStatus.Pending, ExpenseDateUtc = targetDate },   // Excluded
            new Expense { Amount = 200m, Category = "Bills", Status = ExpenseStatus.Failed, ExpenseDateUtc = targetDate },        // Excluded
            new Expense { Amount = 350m, Category = "Food", Status = ExpenseStatus.Cancelled, ExpenseDateUtc = targetDate },     // Excluded
            new Expense { Amount = 400m, Category = "Travel", Status = ExpenseStatus.Unknown, ExpenseDateUtc = targetDate },     // Excluded
        });

        var summary = await _reportService.GetMonthlySummaryAsync(2026, 8);

        // Only 500 (Paid) + 150 (Recorded) = 650
        Assert.Equal(650m, summary.TotalAmount);
        Assert.Equal(2, summary.TransactionCount);
        Assert.Equal(325m, summary.AverageAmount);

        Assert.Equal(2, summary.CategoryTotals.Count);
        var foodCat = summary.CategoryTotals.First(c => c.CategoryName == "Food");
        Assert.Equal(500m, foodCat.TotalAmount);
        Assert.Equal(76.9, foodCat.Percentage); // 500 / 650 = 76.92%

        var travelCat = summary.CategoryTotals.First(c => c.CategoryName == "Travel");
        Assert.Equal(150m, travelCat.TotalAmount);
        Assert.Equal(23.1, travelCat.Percentage); // 150 / 650 = 23.08%
    }

    [Fact]
    public async Task GetMonthlySummary_EmptyMonth_ReturnsZeroSummary()
    {
        var summary = await _reportService.GetMonthlySummaryAsync(2026, 5);

        Assert.Equal(0m, summary.TotalAmount);
        Assert.Equal(0, summary.TransactionCount);
        Assert.Equal(0m, summary.AverageAmount);
        Assert.Empty(summary.CategoryTotals);
        Assert.Empty(summary.PaymentMethodTotals);
    }

    [Fact]
    public async Task GetMonthlySummary_RespectsExpenseDate_NotEntryDate()
    {
        // An expense created in August with ExpenseDate set to July
        var julyDate = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);
        var augCreated = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        _expenseRepo.Expenses.Add(new Expense
        {
            Amount = 450m,
            Category = "Groceries",
            Status = ExpenseStatus.Recorded,
            CreatedAtUtc = augCreated,
            ExpenseDateUtc = julyDate,
            PaymentMethod = PaymentMethod.Cash
        });

        var julySummary = await _reportService.GetMonthlySummaryAsync(2026, 7);
        var augSummary = await _reportService.GetMonthlySummaryAsync(2026, 8);

        Assert.Equal(450m, julySummary.TotalAmount);
        Assert.Equal(1, julySummary.TransactionCount);

        Assert.Equal(0m, augSummary.TotalAmount);
        Assert.Equal(0, augSummary.TransactionCount);
    }
}
