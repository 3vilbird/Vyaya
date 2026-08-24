using System.Globalization;
using Vyaya.Data;
using Vyaya.Models;

namespace Vyaya.Services;

public class ExpenseReportService : IExpenseReportService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;

    private static readonly string[] DynamicColorPalette = new[]
    {
        "#FF9500", // Vibrant Orange (Food)
        "#34C759", // Leaf Green (Vegetables)
        "#30B0C7", // Cyan Teal (Groceries)
        "#FF2D55", // Rose Pink (Restaurants)
        "#007AFF", // Electric Blue (Travel)
        "#FF9F0A", // Amber Orange (Fuel)
        "#AF52DE", // Violet Purple (Shopping)
        "#5856D6", // Indigo (Bills)
        "#FFCC00", // Golden Yellow (Utilities)
        "#FF3B30", // Crimson Red (Entertainment)
        "#32D74B", // Mint Green (Health)
        "#64D2FF", // Sky Blue (Education)
        "#A2845E", // Warm Clay (Rent)
        "#BF5AF2", // Magenta (Subscriptions)
        "#8E8E93"  // Slate Gray (Other)
    };

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

        // Group by category with guaranteed distinct vibrant colors
        var categoryTotals = validExpenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Other" : e.Category.Trim())
            .Select(g =>
            {
                var catName = g.Key;
                categoryMap.TryGetValue(catName, out var catMeta);

                var catTotal = g.Sum(e => e.Amount);
                var percentage = totalAmount > 0 ? (double)(catTotal / totalAmount * 100m) : 0;

                var icon = !string.IsNullOrEmpty(catMeta?.Icon) ? catMeta.Icon : GetDefaultIcon(catName);
                var colorHex = !string.IsNullOrEmpty(catMeta?.ColorHex) ? catMeta.ColorHex : GetDefaultColor(catName);

                return new CategoryExpenseSummary
                {
                    CategoryName = catName,
                    TotalAmount = catTotal,
                    Percentage = Math.Round(percentage, 1),
                    Count = g.Count(),
                    Icon = icon,
                    ColorHex = colorHex
                };
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Group by payment method with distinct colors
        var paymentMethodTotals = validExpenses
            .GroupBy(e => e.PaymentMethod)
            .Select(g =>
            {
                var method = g.Key;
                var methodTotal = g.Sum(e => e.Amount);
                var percentage = totalAmount > 0 ? (double)(methodTotal / totalAmount * 100m) : 0;

                var (icon, colorHex) = method switch
                {
                    PaymentMethod.Cash => ("💵", "#34C759"),
                    PaymentMethod.Upi => ("⚡", "#5856D6"),
                    PaymentMethod.CreditCard => ("💳", "#FF9500"),
                    PaymentMethod.DebitCard => ("💳", "#0A84FF"),
                    PaymentMethod.BankTransfer => ("🏦", "#30B0C7"),
                    _ => ("📦", "#8E8E93")
                };

                return new PaymentMethodExpenseSummary
                {
                    PaymentMethod = method,
                    Name = method.ToString(),
                    TotalAmount = methodTotal,
                    Percentage = Math.Round(percentage, 1),
                    Count = g.Count(),
                    Icon = icon,
                    ColorHex = colorHex
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

    private static string GetDefaultColor(string categoryName)
    {
        var lower = categoryName.Trim().ToLowerInvariant();
        return lower switch
        {
            "food" => "#FF9500",           // Vibrant Orange
            "vegetables" => "#34C759",     // Bright Green
            "groceries" => "#30B0C7",      // Cyan / Teal
            "restaurants" => "#FF2D55",    // Pink / Rose
            "travel" => "#007AFF",         // Apple Blue
            "fuel" => "#FF9F0A",           // Amber Orange
            "shopping" => "#AF52DE",       // Violet Purple
            "bills" => "#5856D6",          // Indigo
            "utilities" => "#FFCC00",      // Golden Yellow
            "entertainment" => "#FF3B30",  // Crimson Red
            "health" => "#32D74B",         // Mint Green
            "education" => "#64D2FF",      // Sky Blue
            "rent" => "#A2845E",           // Warm Clay Brown
            "subscriptions" => "#BF5AF2",  // Magenta
            "other" => "#8E8E93",          // Slate Gray
            _ => DynamicColorPalette[Math.Abs(categoryName.GetHashCode()) % DynamicColorPalette.Length]
        };
    }

    private static string GetDefaultIcon(string categoryName)
    {
        var lower = categoryName.Trim().ToLowerInvariant();
        return lower switch
        {
            "food" => "🍔",
            "vegetables" => "🥦",
            "groceries" => "🛒",
            "restaurants" => "🍽️",
            "travel" => "✈️",
            "fuel" => "⛽",
            "shopping" => "🛍️",
            "bills" => "🧾",
            "utilities" => "💡",
            "entertainment" => "🎬",
            "health" => "💊",
            "education" => "📚",
            "rent" => "🏠",
            "subscriptions" => "📱",
            "other" => "📦",
            _ => "📁"
        };
    }
}
