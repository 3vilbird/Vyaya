using System.Globalization;

namespace Vyaya.Models;

public class MonthlyExpenseSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageAmount { get; set; }

    public List<CategoryExpenseSummary> CategoryTotals { get; set; } = new();
    public List<PaymentMethodExpenseSummary> PaymentMethodTotals { get; set; } = new();

    public string FormattedTotalAmount
    {
        get
        {
            var culture = new CultureInfo("en-IN");
            return TotalAmount % 1 == 0
                ? $"₹{TotalAmount.ToString("N0", culture)}"
                : $"₹{TotalAmount.ToString("N2", culture)}";
        }
    }

    public string FormattedAverageAmount
    {
        get
        {
            var culture = new CultureInfo("en-IN");
            return $"₹{AverageAmount.ToString("N2", culture)}";
        }
    }
}

public class CategoryExpenseSummary
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
    public int Count { get; set; }
    public string Icon { get; set; } = "📁";
    public string ColorHex { get; set; } = "#0A84FF";

    public string FormattedAmount
    {
        get
        {
            var culture = new CultureInfo("en-IN");
            return TotalAmount % 1 == 0
                ? $"₹{TotalAmount.ToString("N0", culture)}"
                : $"₹{TotalAmount.ToString("N2", culture)}";
        }
    }

    public string FormattedPercentage => $"{Percentage:0.0}%";
}

public class PaymentMethodExpenseSummary
{
    public PaymentMethod PaymentMethod { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
    public int Count { get; set; }
    public string Icon { get; set; } = "💳";

    public string FormattedAmount
    {
        get
        {
            var culture = new CultureInfo("en-IN");
            return TotalAmount % 1 == 0
                ? $"₹{TotalAmount.ToString("N0", culture)}"
                : $"₹{TotalAmount.ToString("N2", culture)}";
        }
    }

    public string FormattedPercentage => $"{Percentage:0.0}%";
}
