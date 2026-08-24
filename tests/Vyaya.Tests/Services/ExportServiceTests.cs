using Vyaya.Models;
using Vyaya.Services;
using Xunit;

namespace Vyaya.Tests.Services;

public class ExportServiceTests
{
    private readonly ExportService _exportService = new();

    [Fact]
    public async Task GenerateExportCsv_ContainsRequiredHeadersAndEscapesProperly()
    {
        var expenses = new List<Expense>
        {
            new()
            {
                Amount = 450m,
                MerchantName = "ABC \"Super\" Store, Market",
                Category = "Groceries",
                PaymentMethod = PaymentMethod.Upi,
                EntryType = ExpenseEntryType.UpiScan,
                Status = ExpenseStatus.Paid,
                UpiId = "abc@upi",
                PaymentReference = "TXN999",
                Note = "Weekly supplies, fruits",
                CreatedAtUtc = new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Amount = 180m,
                Category = "Food",
                PaymentMethod = PaymentMethod.Cash,
                EntryType = ExpenseEntryType.Manual,
                Status = ExpenseStatus.Recorded,
                Note = "Lunch with team",
                CreatedAtUtc = new DateTime(2026, 8, 23, 13, 30, 0, DateTimeKind.Utc)
            }
        };

        var csv = await _exportService.GenerateExportCsvAsync(expenses);

        Assert.NotNull(csv);
        // Header verification
        Assert.Contains("Date,Merchant / Description,Amount,Currency,Category,Payment Method,Entry Type,Note,UPI ID,Payment Reference,Status", csv);
        
        // Content verification
        Assert.Contains("\"23/08/2026\"", csv);
        Assert.Contains("450.00", csv);
        Assert.Contains("ABC \"\"Super\"\" Store, Market", csv);
        Assert.Contains("180.00", csv);
        Assert.Contains("Recorded", csv);
        Assert.Contains("Paid", csv);
    }
}
