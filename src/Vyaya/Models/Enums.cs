namespace Vyaya.Models;

public enum ExpenseEntryType
{
    UpiScan = 0,
    Manual = 1
}

public enum PaymentMethod
{
    Cash = 0,
    Upi = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    Other = 5
}

public enum ExpenseStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Cancelled = 3,
    Unknown = 4,
    Recorded = 5
}
