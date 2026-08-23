using Vyaya.Models;

namespace Vyaya.Services;

public interface IExportService
{
    Task<string> GenerateExportCsvAsync(List<Expense> expenses);
    Task<string> ExportExpensesToFileAsync(List<Expense> expenses, string fileNamePrefix = "Vyaya_Expenses");
    Task<bool> ShareExportedFileAsync(string filePath, string title = "Export Expenses");
}
