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
    /// Additional original NPCI parameters from scanned QR (e.g., mc, tr, mode, orgid, sign, mid, msid, mtid, url).
    /// Preserving these is mandatory for verified corporate/retail merchants (e.g. Apollo Pharmacy, Reliance, DMart).
    /// </summary>
    public Dictionary<string, string> AdditionalParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Builds a compliant NPCI UPI Intent URI.
    /// Preserves all original merchant parameters (mc, tr, mode, orgid, sign, etc.)
    /// while updating or appending user-specified amount and notes.
    /// </summary>
    public string BuildUpiUri(decimal? overrideAmount = null)
    {
        var finalAmount = overrideAmount ?? Amount;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. Copy all original parameters first (preserves mode, orgid, sign, mid, etc.)
        foreach (var kvp in AdditionalParameters)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
            {
                map[kvp.Key] = kvp.Value;
            }
        }

        // 2. pa: Payee VPA / UPI ID (Required)
        if (!string.IsNullOrWhiteSpace(PaymentAddress))
        {
            map["pa"] = PaymentAddress.Trim();
        }

        // 3. pn: Payee Name
        if (!string.IsNullOrWhiteSpace(PayeeName))
        {
            map["pn"] = PayeeName.Trim();
        }

        // 4. mc: Merchant Category Code
        if (!string.IsNullOrWhiteSpace(MerchantCode))
        {
            map["mc"] = MerchantCode.Trim();
        }

        // 5. tr: Transaction / Invoice Reference
        if (!string.IsNullOrWhiteSpace(TransactionReference))
        {
            map["tr"] = TransactionReference.Trim();
        }

        // 6. am: Amount (0.00 format)
        if (finalAmount.HasValue && finalAmount.Value > 0)
        {
            map["am"] = finalAmount.Value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        // 7. cu: Currency (Always INR for UPI unless specified)
        if (!string.IsNullOrWhiteSpace(Currency))
        {
            map["cu"] = Currency.Trim().ToUpperInvariant();
        }
        else if (!map.ContainsKey("cu"))
        {
            map["cu"] = "INR";
        }

        // 8. tn: Transaction Note (if provided or present in original QR)
        if (!string.IsNullOrWhiteSpace(TransactionNote))
        {
            map["tn"] = TransactionNote.Trim();
        }

        // Format query string with proper URL encoding (pa preserves literal '@' per UPI intent spec)
        var queryParts = map.Select(kvp =>
        {
            if (kvp.Key.Equals("pa", StringComparison.OrdinalIgnoreCase))
            {
                return $"pa={kvp.Value}";
            }
            return $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}";
        });
        return $"upi://pay?{string.Join("&", queryParts)}";
    }
}
