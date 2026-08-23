using Vyaya.Models;

namespace Vyaya.Services;

public interface IExpenseReportService
{
    Task<MonthlyExpenseSummary> GetMonthlySummaryAsync(int year, int month);
    Task<List<MonthlyExpenseSummary>> GetAnnualSummaryAsync(int year);
}
