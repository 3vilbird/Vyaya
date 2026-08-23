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
    /// Builds a clean, compliant NPCI/UPI Intent URI.
    /// Excludes restricted internal merchant signature tags (e.g. mode=02, orgid, sign)
    /// that trigger NPCI/PhonePe "Payment denied for security reasons" on third-party app intents.
    /// </summary>
    public string BuildUpiUri(decimal? overrideAmount = null)
    {
        var finalAmount = overrideAmount ?? Amount;
        var queryParams = new List<string>();

        // 1. Payee Address (VPA) - Required
        if (!string.IsNullOrWhiteSpace(PaymentAddress))
        {
            queryParams.Add($"pa={Uri.EscapeDataString(PaymentAddress.Trim())}");
        }

        // 2. Payee Name - Optional but recommended
        if (!string.IsNullOrWhiteSpace(PayeeName))
        {
            queryParams.Add($"pn={Uri.EscapeDataString(PayeeName.Trim())}");
        }

        // 3. Amount - Formatted as 0.00
        if (finalAmount.HasValue && finalAmount.Value > 0)
        {
            var amountStr = finalAmount.Value.ToString("0.00", CultureInfo.InvariantCulture);
            queryParams.Add($"am={amountStr}");
        }

        // 4. Currency - Always INR for UPI
        var cur = !string.IsNullOrWhiteSpace(Currency) ? Currency.Trim().ToUpperInvariant() : "INR";
        queryParams.Add($"cu={cur}");

        // 5. Transaction Note / Description
        if (!string.IsNullOrWhiteSpace(TransactionNote))
        {
            queryParams.Add($"tn={Uri.EscapeDataString(TransactionNote.Trim())}");
        }

        // 6. Merchant Code (MCC) - 4 digit standard code
        if (!string.IsNullOrWhiteSpace(MerchantCode) && MerchantCode.Trim().Length == 4 && char.IsDigit(MerchantCode.Trim()[0]))
        {
            queryParams.Add($"mc={Uri.EscapeDataString(MerchantCode.Trim())}");
        }

        // 7. Transaction Reference (if present and clean)
        if (!string.IsNullOrWhiteSpace(TransactionReference))
        {
            queryParams.Add($"tr={Uri.EscapeDataString(TransactionReference.Trim())}");
        }

        return $"upi://pay?{string.Join("&", queryParams)}";
    }
}
