using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using Vyaya.Models;

namespace Vyaya.Services;

public class UpiParser : IUpiParser
{
    private static readonly Regex VpaRegex = new(@"^[a-zA-Z0-9.\-_]{2,256}@[a-zA-Z]{2,64}$", RegexOptions.Compiled);

    public bool IsValidUpiPayload(string payload)
    {
        return Parse(payload) != null;
    }

    public UpiPaymentRequest? Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var trimmed = payload.Trim();

        // 1. Direct or embedded upi://pay URI
        var upiSchemeIndex = trimmed.IndexOf("upi://pay", StringComparison.OrdinalIgnoreCase);
        if (upiSchemeIndex >= 0)
        {
            var upiSubstring = trimmed[upiSchemeIndex..];
            var hashIndex = upiSubstring.IndexOf('#');
            if (hashIndex > 0)
            {
                upiSubstring = upiSubstring[..hashIndex];
            }
            return ParseUpiQueryString(upiSubstring, trimmed);
        }

        // 2. Custom app schemes (phonepe://pay?, paytmmp://pay?, gpay://upi/pay?, bhim://pay?, intent://pay?, etc.)
        if (trimmed.StartsWith("phonepe://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("paytmmp://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("gpay://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("bhim://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("intent://", StringComparison.OrdinalIgnoreCase))
        {
            var cleanUri = trimmed;
            var hashIndex = cleanUri.IndexOf('#');
            if (hashIndex > 0)
            {
                cleanUri = cleanUri[..hashIndex];
            }
            return ParseUpiQueryString(cleanUri, trimmed);
        }

        // 3. Web URLs containing UPI query parameters (e.g., https://phonepe.com/pay?pa=... or https://upiqr.in/pay?pa=...)
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (trimmed.Contains("pa=", StringComparison.OrdinalIgnoreCase))
            {
                return ParseUpiQueryString(trimmed, trimmed);
            }
        }

        // 4. EMVCo / BharatQR payload containing UPI sub-tags (Tag 26/27 with VPA or upi://)
        if (trimmed.StartsWith("000201", StringComparison.OrdinalIgnoreCase))
        {
            var emvcoResult = ParseEmvcoPayload(trimmed);
            if (emvcoResult != null)
                return emvcoResult;
        }

        // 5. Bare VPA address (e.g. 9876543210@ybl, merchant@okaxis, shop@paytm)
        if (VpaRegex.IsMatch(trimmed))
        {
            return new UpiPaymentRequest
            {
                PaymentAddress = trimmed,
                Currency = "INR",
                RawPayload = $"upi://pay?pa={trimmed}&cu=INR"
            };
        }

        return null;
    }

    private static UpiPaymentRequest? ParseUpiQueryString(string upiUri, string originalPayload)
    {
        try
        {
            var queryStartIndex = upiUri.IndexOf('?');
            if (queryStartIndex < 0 || queryStartIndex >= upiUri.Length - 1)
                return null;

            var queryString = upiUri[(queryStartIndex + 1)..];
            var queryDictionary = ParseQueryString(queryString);

            // 'pa' (Payee Address / VPA) is required for a valid UPI payment QR
            if (!queryDictionary.TryGetValue("pa", out var paymentAddress) || string.IsNullOrWhiteSpace(paymentAddress))
            {
                return null;
            }

            var request = new UpiPaymentRequest
            {
                PaymentAddress = paymentAddress.Trim(),
                RawPayload = originalPayload
            };

            if (queryDictionary.TryGetValue("pn", out var payeeName) && !string.IsNullOrWhiteSpace(payeeName))
            {
                request.PayeeName = payeeName.Trim();
            }

            if (queryDictionary.TryGetValue("am", out var amountStr) && !string.IsNullOrWhiteSpace(amountStr))
            {
                var cleanAmount = amountStr.Replace(",", "").Trim();
                if (decimal.TryParse(cleanAmount, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedAmount))
                {
                    if (parsedAmount > 0)
                    {
                        request.Amount = parsedAmount;
                    }
                }
            }

            if (queryDictionary.TryGetValue("cu", out var currency) && !string.IsNullOrWhiteSpace(currency))
            {
                request.Currency = currency.Trim().ToUpperInvariant();
            }
            else
            {
                request.Currency = "INR";
            }

            if (queryDictionary.TryGetValue("tr", out var txnRef) && !string.IsNullOrWhiteSpace(txnRef))
            {
                request.TransactionReference = txnRef.Trim();
            }

            if (queryDictionary.TryGetValue("tn", out var txnNote) && !string.IsNullOrWhiteSpace(txnNote))
            {
                request.TransactionNote = txnNote.Trim();
            }

            if (queryDictionary.TryGetValue("mc", out var merchantCode) && !string.IsNullOrWhiteSpace(merchantCode))
            {
                request.MerchantCode = merchantCode.Trim();
            }

            return request;
        }
        catch
        {
            return null;
        }
    }

    private static UpiPaymentRequest? ParseEmvcoPayload(string emvco)
    {
        try
        {
            var vpaMatch = Regex.Match(emvco, @"[a-zA-Z0-9.\-_]{2,64}@[a-zA-Z]{2,32}");
            if (vpaMatch.Success)
            {
                return new UpiPaymentRequest
                {
                    PaymentAddress = vpaMatch.Value,
                    Currency = "INR",
                    RawPayload = emvco
                };
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex < 0)
                continue;

            var key = pair[..eqIndex].Trim();
            var rawValue = pair[(eqIndex + 1)..];

            var decodedValue = Uri.UnescapeDataString(rawValue.Replace("+", " "));

            if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
            {
                result[key] = decodedValue;
            }
        }

        return result;
    }
}
