namespace Vyaya.Models;

public class UpiPaymentResult
{
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Unknown;

    public string? TransactionReference { get; set; }

    public string? ApprovalRefNo { get; set; }

    public string? ResponseCode { get; set; }

    public string? RawResponse { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsSuccess => Status == ExpenseStatus.Paid;

    public static UpiPaymentResult Success(string? txnRef = null, string? approvalRef = null, string? raw = null) =>
        new()
        {
            Status = ExpenseStatus.Paid,
            TransactionReference = txnRef,
            ApprovalRefNo = approvalRef,
            RawResponse = raw
        };

    public static UpiPaymentResult Failed(string? error = null, string? raw = null) =>
        new()
        {
            Status = ExpenseStatus.Failed,
            ErrorMessage = error,
            RawResponse = raw
        };

    public static UpiPaymentResult Cancelled(string? raw = null) =>
        new()
        {
            Status = ExpenseStatus.Cancelled,
            RawResponse = raw
        };

    public static UpiPaymentResult Unknown(string? raw = null, string? note = null) =>
        new()
        {
            Status = ExpenseStatus.Unknown,
            RawResponse = raw,
            ErrorMessage = note
        };
}
