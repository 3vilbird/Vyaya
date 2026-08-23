using System.Text.RegularExpressions;

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

    public string BuildUpiUri(decimal? overrideAmount = null)
    {
        var finalAmount = overrideAmount ?? Amount;

        // If we have an existing RawPayload starting with upi://pay, preserve all original merchant parameters (orgid, mode, etc.)
        if (!string.IsNullOrWhiteSpace(RawPayload) && RawPayload.Trim().StartsWith("upi://pay", StringComparison.OrdinalIgnoreCase))
        {
            var raw = RawPayload.Trim();
            if (finalAmount.HasValue && finalAmount.Value > 0)
            {
                var amountStr = finalAmount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                if (raw.Contains("am=", StringComparison.OrdinalIgnoreCase))
                {
                    raw = Regex.Replace(raw, @"(?i)am=[^&]*", $"am={amountStr}");
                }
                else
                {
                    raw = raw.Contains('?') ? $"{raw}&am={amountStr}" : $"{raw}?am={amountStr}";
                }
            }
            return raw;
        }

        // Otherwise build clean standard upi://pay URI
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(PaymentAddress))
            queryParams.Add($"pa={Uri.EscapeDataString(PaymentAddress)}");

        if (!string.IsNullOrWhiteSpace(PayeeName))
            queryParams.Add($"pn={Uri.EscapeDataString(PayeeName)}");

        if (finalAmount.HasValue && finalAmount.Value > 0)
            queryParams.Add($"am={finalAmount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");

        var cur = !string.IsNullOrWhiteSpace(Currency) ? Currency : "INR";
        queryParams.Add($"cu={Uri.EscapeDataString(cur)}");

        if (!string.IsNullOrWhiteSpace(TransactionReference))
            queryParams.Add($"tr={Uri.EscapeDataString(TransactionReference)}");

        if (!string.IsNullOrWhiteSpace(TransactionNote))
            queryParams.Add($"tn={Uri.EscapeDataString(TransactionNote)}");

        if (!string.IsNullOrWhiteSpace(MerchantCode))
            queryParams.Add($"mc={Uri.EscapeDataString(MerchantCode)}");

        return $"upi://pay?{string.Join("&", queryParams)}";
    }
}
