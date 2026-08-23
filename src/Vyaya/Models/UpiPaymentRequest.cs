using System.Globalization;

namespace Vyaya.Models;

public class UpiPaymentRequest
{
    public string? PaymentAddress { get; set; }

    public string? PayeeName { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? TransactionReference { get; set; }

    public string? TransactionNote { get; set; }

    public string? MerchantCode { get; set; }

    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    /// Builds a clean, universal NPCI-compliant UPI Intent URI.
    /// Strictly passes the 5 official standard parameters:
    /// • pa: Verified Payee UPI ID (VPA)
    /// • pn: Verified Payee Name
    /// • am: Exact Amount (0.00 format)
    /// • cu: INR
    /// • tn: Transaction Note
    /// </summary>
    public string BuildUpiUri(decimal? overrideAmount = null)
    {
        var finalAmount = overrideAmount ?? Amount;
        var queryParams = new List<string>();

        // 1. pa: Verified Payee UPI ID (VPA) - Required
        if (!string.IsNullOrWhiteSpace(PaymentAddress))
        {
            queryParams.Add($"pa={PaymentAddress.Trim()}");
        }

        // 2. pn: Verified Payee Name - Formatted clean
        var name = !string.IsNullOrWhiteSpace(PayeeName) ? PayeeName.Trim() : (PaymentAddress?.Split('@')[0] ?? "Merchant");
        queryParams.Add($"pn={Uri.EscapeDataString(name)}");

        // 3. am: Exact Amount (0.00 format)
        if (finalAmount.HasValue && finalAmount.Value > 0)
        {
            var amountStr = finalAmount.Value.ToString("0.00", CultureInfo.InvariantCulture);
            queryParams.Add($"am={amountStr}");
        }

        // 4. cu: Currency (Always INR for UPI)
        queryParams.Add("cu=INR");

        // 5. tn: Transaction Note / Description
        var note = !string.IsNullOrWhiteSpace(TransactionNote) ? TransactionNote.Trim() : "Vyaya Payment";
        queryParams.Add($"tn={Uri.EscapeDataString(note)}");

        return $"upi://pay?{string.Join("&", queryParams)}";
    }
}
