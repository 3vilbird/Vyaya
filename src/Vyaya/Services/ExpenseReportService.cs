using System.Globalization;
using Vyaya.Data;
using Vyaya.Models;

namespace Vyaya.Services;

public class ExpenseReportService : IExpenseReportService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ExpenseReportService(IExpenseRepository expenseRepository, ICategoryRepository categoryRepository)
    {
        _expenseRepository = expenseRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<MonthlyExpenseSummary> GetMonthlySummaryAsync(int year, int month)
    {
        var rawExpenses = await _expenseRepository.GetByMonthAsync(year, month);
        var categories = await _categoryRepository.GetAllAsync();
        var categoryMap = categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        // Section 24: Spending includes 'Paid' and 'Recorded', excludes 'Pending', 'Failed', 'Cancelled', 'Unknown'
        var validExpenses = rawExpenses
            .Where(e => e.Status == ExpenseStatus.Paid || e.Status == ExpenseStatus.Recorded)
            .ToList();

        var monthDate = new DateTime(year, month, 1);
        var monthName = monthDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        var totalAmount = validExpenses.Sum(e => e.Amount);
        var count = validExpenses.Count;
        var averageAmount = count > 0 ? Math.Round(totalAmount / count, 2) : 0m;

        // Group by category
        var categoryTotals = validExpenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Other" : e.Category)
            .Select(g =>
            {
                var catName = g.Key;
                categoryMap.TryGetValue(catName, out var catMeta);

                var catTotal = g.Sum(e => e.Amount);
                var percentage = totalAmount > 0 ? (double)(catTotal / totalAmount * 100m) : 0;

                return new CategoryExpenseSummary
                {
                    CategoryName = catName,
                    TotalAmount = catTotal,
                    Percentage = Math.Round(percentage, 1),
                    Count = g.Count(),
                    Icon = catMeta?.Icon ?? "📁",
                    ColorHex = catMeta?.ColorHex ?? "#007AFF"
                };
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Group by payment method
        var paymentMethodTotals = validExpenses
            .GroupBy(e => e.PaymentMethod)
            .Select(g =>
            {
                var method = g.Key;
                var methodTotal = g.Sum(e => e.Amount);
                var percentage = totalAmount > 0 ? (double)(methodTotal / totalAmount * 100m) : 0;

                var icon = method switch
                {
                    PaymentMethod.Cash => "💵",
                    PaymentMethod.Upi => "⚡",
                    PaymentMethod.CreditCard => "💳",
                    PaymentMethod.DebitCard => "💳",
                    PaymentMethod.BankTransfer => "🏦",
                    _ => "📦"
                };

                return new PaymentMethodExpenseSummary
                {
                    PaymentMethod = method,
                    Name = method.ToString(),
                    TotalAmount = methodTotal,
                    Percentage = Math.Round(percentage, 1),
                    Count = g.Count(),
                    Icon = icon
                };
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        return new MonthlyExpenseSummary
        {
            Year = year,
            Month = month,
            MonthName = monthName,
            TotalAmount = totalAmount,
            TransactionCount = count,
            AverageAmount = averageAmount,
            CategoryTotals = categoryTotals,
            PaymentMethodTotals = paymentMethodTotals
        };
    }

    public async Task<List<MonthlyExpenseSummary>> GetAnnualSummaryAsync(int year)
    {
        var summaries = new List<MonthlyExpenseSummary>();
        for (int m = 1; m <= 12; m++)
        {
            var summary = await GetMonthlySummaryAsync(year, m);
            summaries.Add(summary);
        }
        return summaries;
    }
}
