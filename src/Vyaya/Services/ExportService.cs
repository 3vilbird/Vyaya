using System.Globalization;
using System.Text;
using Vyaya.Models;

namespace Vyaya.Services;

public class ExportService : IExportService
{
    public Task<string> GenerateExportCsvAsync(List<Expense> expenses)
    {
        var sb = new StringBuilder();

        // UTF-8 BOM for flawless Excel opening
        sb.Append('\uFEFF');

        // Headers as requested by AGENTS.md Section 27
        sb.AppendLine("Date,Merchant / Description,Amount,Currency,Category,Payment Method,Entry Type,Note,UPI ID,Payment Reference,Status");

        foreach (var expense in expenses.OrderByDescending(e => e.ExpenseDateUtc ?? e.CreatedAtUtc))
        {
            var dateStr = (expense.ExpenseDateUtc ?? expense.CreatedAtUtc).ToLocalTime().ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
            var description = EscapeCsv(expense.DisplayTitle);
            var amountStr = expense.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            var currency = EscapeCsv(expense.Currency);
            var category = EscapeCsv(expense.Category ?? "Other");
            var method = EscapeCsv(expense.PaymentMethod.ToString());
            var entryType = EscapeCsv(expense.EntryType == ExpenseEntryType.UpiScan ? "UPI Scan" : "Manual");
            var note = EscapeCsv(expense.Note ?? string.Empty);
            var upiId = EscapeCsv(expense.UpiId ?? string.Empty);
            var reference = EscapeCsv(expense.PaymentReference ?? string.Empty);
            var status = EscapeCsv(expense.Status.ToString());

            sb.AppendLine($"{dateStr},{description},{amountStr},{currency},{category},{method},{entryType},{note},{upiId},{reference},{status}");
        }

        return Task.FromResult(sb.ToString());
    }

    public async Task<string> ExportExpensesToFileAsync(List<Expense> expenses, string fileNamePrefix = "Vyaya_Expenses")
    {
        var csvContent = await GenerateExportCsvAsync(expenses);
        var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{fileNamePrefix}_{timeStamp}.csv";
        var targetPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        await File.WriteAllTextAsync(targetPath, csvContent, Encoding.UTF8);
        return targetPath;
    }

    public async Task<bool> ShareExportedFileAsync(string filePath, string title = "Export Expenses")
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(filePath, "text/csv")
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var mustQuote = value.Contains(',') || value.Contains('\"') || value.Contains('\r') || value.Contains('\n');
        var escaped = value.Replace("\"", "\"\"");

        return mustQuote ? $"\"{escaped}\"" : $"\"{escaped}\"";
    }
}
