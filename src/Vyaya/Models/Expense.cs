using System.Globalization;
using SQLite;

namespace Vyaya.Models;

[Table("Expenses")]
public class Expense
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Indexed]
    public DateTime? ExpenseDateUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    [NotNull]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "INR";

    [Indexed, MaxLength(250)]
    public string? MerchantName { get; set; }

    [MaxLength(250)]
    public string? UpiId { get; set; }

    public string? Note { get; set; }

    public string? UpiTransactionNote { get; set; }

    [Indexed, MaxLength(100)]
    public string? Category { get; set; }

    public string? RawUpiPayload { get; set; }

    [Indexed]
    public ExpenseEntryType EntryType { get; set; }

    [Indexed]
    public PaymentMethod PaymentMethod { get; set; }

    [Indexed]
    public ExpenseStatus Status { get; set; }

    [MaxLength(250)]
    public string? PaymentReference { get; set; }

    // Presentation Helpers (Ignored by SQLite)
    [Ignore]
    public DateTime EffectiveDateLocal => (ExpenseDateUtc ?? CreatedAtUtc).ToLocalTime();

    [Ignore]
    public string FormattedDateTime => EffectiveDateLocal.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);

    [Ignore]
    public string FormattedDate => EffectiveDateLocal.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    [Ignore]
    public string FormattedTime => EffectiveDateLocal.ToString("hh:mm tt", CultureInfo.InvariantCulture);

    [Ignore]
    public string FormattedAmount
    {
        get
        {
            var culture = new CultureInfo("en-IN");
            return Amount % 1 == 0 
                ? $"₹{Amount.ToString("N0", culture)}" 
                : $"₹{Amount.ToString("N2", culture)}";
        }
    }

    [Ignore]
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(MerchantName))
                return MerchantName;
            if (!string.IsNullOrWhiteSpace(Category))
                return Category;
            if (!string.IsNullOrWhiteSpace(Note))
                return Note;
            return "Expense";
        }
    }

    [Ignore]
    public string DisplaySubtitle
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Category))
                parts.Add(Category);
            parts.Add(PaymentMethod.ToString());
            parts.Add(FormattedTime);
            if (!string.IsNullOrWhiteSpace(Note) && DisplayTitle != Note)
                parts.Add(Note);
            return string.Join(" • ", parts);
        }
    }

    [Ignore]
    public string StatusText => Status switch
    {
        ExpenseStatus.Paid => "Paid",
        ExpenseStatus.Recorded => "Recorded",
        ExpenseStatus.Pending => "Pending",
        ExpenseStatus.Failed => "Failed",
        ExpenseStatus.Cancelled => "Cancelled",
        ExpenseStatus.Unknown => "Unknown",
        _ => Status.ToString()
    };

    [Ignore]
    public string StatusColor => Status switch
    {
        ExpenseStatus.Paid => "#34C759",      // iOS Green
        ExpenseStatus.Recorded => "#007AFF",  // iOS Blue
        ExpenseStatus.Pending => "#FF9500",   // iOS Amber
        ExpenseStatus.Failed => "#FF3B30",    // iOS Red
        ExpenseStatus.Cancelled => "#8E8E93", // iOS Gray
        ExpenseStatus.Unknown => "#AF52DE",   // iOS Purple
        _ => "#8E8E93"
    };

    [Ignore]
    public string StatusBackground => Status switch
    {
        ExpenseStatus.Paid => "#1A34C759",
        ExpenseStatus.Recorded => "#1A007AFF",
        ExpenseStatus.Pending => "#1AFF9500",
        ExpenseStatus.Failed => "#1AFF3B30",
        ExpenseStatus.Cancelled => "#1A8E8E93",
        ExpenseStatus.Unknown => "#1AAF52DE",
        _ => "#1A8E8E93"
    };

    [Ignore]
    public string PaymentMethodBadge => PaymentMethod switch
    {
        PaymentMethod.Cash => "💵 Cash",
        PaymentMethod.Upi => "⚡ UPI",
        PaymentMethod.CreditCard => "💳 Credit Card",
        PaymentMethod.DebitCard => "💳 Debit Card",
        PaymentMethod.BankTransfer => "🏦 Bank Transfer",
        _ => "📦 Other"
    };
}
