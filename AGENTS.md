# AGENTS.md

# Vyaya — Local-First Expense Tracker

## Project Overview

Build a **.NET 10 MAUI expense-tracking Android application** called **Vyaya**.

**Vyaya (व्यय)** is a Sanskrit-derived word associated with **expenditure, spending, or expense**.

The application is a **local-first personal expense tracker**.

Its primary differentiator is that users can quickly record an expense by scanning a UPI QR code and then continue the payment through PhonePe or another UPI application.

However, **Vyaya must not depend on UPI or PhonePe**.

Users must also be able to manually record expenses for payments made through:

- Cash
- Card
- Bank transfer
- Other UPI applications where QR scanning was not used
- Any other payment method

The application therefore has two primary expense-entry flows:

```text
                         Vyaya
                           |
              ┌────────────┴────────────┐
              │                         │
              ▼                         ▼
        Scan & Pay                 Add Manually
              │                         │
              ▼                         ▼
         UPI QR Code              Enter Expense
              │                         │
              ▼                         │
        Parse UPI data                   │
              │                         │
              ▼                         │
       Add note/category                 │
              │                         │
              ▼                         │
       Save as Pending                   │
              │                         │
              ▼                         │
       Launch UPI App                    │
              │                         │
              ▼                         │
        Payment result                   │
              │                         │
              └────────────┬────────────┘
                           ▼
                    Expense History
                           │
             ┌─────────────┼─────────────┐
             ▼             ▼             ▼
          Monthly        Charts        Export
          Summary
```

---

# 1. Product Philosophy

Vyaya is an **expense tracker first**, not a payment application.

The application must never:

- Process payments itself.
- Handle UPI PINs.
- Handle bank credentials.
- Authenticate bank transactions.
- Replace PhonePe or another UPI application.

The payment application is responsible for the actual payment.

Vyaya is responsible for:

- Capturing expenses.
- Recording payment information.
- Categorizing expenses.
- Maintaining local history.
- Providing analytics.
- Exporting data.

---

# 2. Platform

Primary platform:

- Android

Technology:

- .NET 10
- .NET MAUI
- C#
- SQLite

The initial implementation should focus primarily on Android.

iOS should not receive significant platform-specific development effort during the initial version unless required by the architecture.

---

# 3. Two Expense Entry Modes

Vyaya must support two independent ways to create an expense.

## Mode A — Scan & Pay

This is the primary and most convenient flow.

```text
Scan QR
   ↓
Decode QR
   ↓
Validate UPI URI
   ↓
Extract payment information
   ↓
Choose category
   ↓
Add optional note
   ↓
Save Pending Expense
   ↓
Launch UPI App
   ↓
Payment
   ↓
Return to Vyaya
   ↓
Update payment status
```

## Mode B — Add Expense Manually

The user must be able to record an expense without scanning a QR code.

Example:

```text
Open Vyaya
   ↓
Tap "Add Expense"
   ↓
Enter amount
   ↓
Choose payment method
   ↓
Choose category
   ↓
Add optional note
   ↓
Save
```

This is required for expenses such as:

```text
Cash payment
Card payment
Offline payment
Bank transfer
UPI payment made outside Vyaya
Payment made through another mechanism
```

---

# 4. Main Home Screen

The Home screen should make both actions easy to access.

Example:

```text
Vyaya

This Month
₹18,450

[ Scan & Pay ]

[ Add Expense ]

Recent Expenses
-------------------------
ABC Vegetables    ₹450
Uber              ₹320
Cash Lunch        ₹180
Amazon            ₹1,250
```

The two primary actions are:

```text
SCAN & PAY
ADD EXPENSE
```

---

# 5. Scan & Pay Flow

The QR payment flow must work as follows:

```text
1. Scan the QR.
2. Decode the QR payload.
3. Recognize that it is a UPI payment URI.
4. Extract useful payment information.
5. Allow the user to add an optional note.
6. Allow the user to search and select a category.
7. Save a Pending Expense locally.
8. Launch the UPI deep link.
9. Let the user choose PhonePe or another UPI application.
10. Return to Vyaya.
11. Determine the most reliable available payment result.
12. Update the expense accordingly.
```

---

# 6. QR Scanning

The application must provide QR scanning.

The scanner should:

- Access the Android camera.
- Decode QR content.
- Validate the decoded content.
- Recognize supported UPI payment URIs.
- Reject unsupported QR codes gracefully.

Example:

```text
upi://pay?pa=merchant@upi&pn=Merchant&am=450.00&cu=INR
```

Not every QR code is a UPI payment QR.

Do not create an expense until the payload has been validated.

---

# 7. UPI Payload Parsing

Create a dedicated UPI parser.

Do not parse UPI information directly inside UI code.

Suggested abstraction:

```csharp
public interface IUpiParser
{
    UpiPaymentRequest? Parse(string payload);
}
```

Suggested model:

```csharp
public class UpiPaymentRequest
{
    public string? PaymentAddress { get; set; }

    public string? PayeeName { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? TransactionReference { get; set; }

    public string? TransactionNote { get; set; }

    public string RawPayload { get; set; } = string.Empty;
}
```

Handle optional UPI fields safely.

Do not assume:

- Amount is always present.
- Merchant name is always present.
- Transaction reference is always present.
- Transaction note is always present.

If the QR does not contain an amount, Vyaya should allow the user to enter the amount before continuing.

---

# 8. Expense Details After QR Scan

After scanning, show the extracted information.

Example:

```text
ABC Vegetables

Amount
₹450

UPI ID
abc@upi

Category
[ Search category ]

Note
[ Optional note ]

[ Pay with UPI ]
```

The user must be able to edit/select:

- Category
- Note

The extracted merchant and payment information should be clearly displayed.

---

# 9. Expense Notes

Provide an optional note field.

Example:

```text
Note

[ Bought vegetables for dinner ]
```

The note is optional.

If the UPI QR contains a transaction note, preserve it separately from the user's own expense note.

Suggested distinction:

```text
UpiTransactionNote
UserNote
```

Do not overwrite one with the other.

---

# 10. Expense Categories

Categories must be available for both:

- Scan & Pay expenses
- Manual expenses

Initial categories should include at least:

```text
Food
Vegetables
Groceries
Restaurants
Travel
Fuel
Shopping
Bills
Utilities
Entertainment
Health
Education
Rent
Subscriptions
Other
```

Categories should be represented as data rather than duplicated throughout the UI.

The architecture should allow categories to evolve later.

---

# 11. Category Search

The category picker must support search.

Example:

```text
Search category

trav

----------------

Travel
```

The user should not have to scroll through a long category list.

Support:

- Search
- Selection
- Clear selection
- Default category such as `Other`

Future versions may support custom categories.

---

# 12. Manual Expense Entry

Manual expense entry is a **first-class feature**, not a workaround.

Provide an `Add Expense` action from the Home screen.

Example:

```text
Add Expense

Amount
[ ₹ 500 ]

Payment Method
[ Cash ▼ ]

Category
[ Search category ]

Note
[ Optional note ]

Date
[ 23 Aug 2026 ]

[ Save Expense ]
```

The manual flow must not require:

- QR scanning
- UPI
- PhonePe
- Internet connectivity
- Payment app availability

---

# 13. Manual Payment Methods

The application should support a payment-method field.

Initial values:

```text
Cash
UPI
Credit Card
Debit Card
Bank Transfer
Other
```

The list should be represented as data/enums rather than hard-coded throughout the UI.

For manually entered expenses, the user selects the appropriate payment method.

Examples:

```text
₹500
Cash
Groceries
```

```text
₹1,200
Credit Card
Shopping
```

```text
₹750
Bank Transfer
Bills
```

```text
₹300
UPI
Food
```

---

# 14. Manual Expense Status

Manual expenses are different from Scan & Pay expenses.

There is no payment attempt that Vyaya initiated.

Therefore a manually entered expense should normally be recorded as:

```text
Recorded
```

or equivalent.

The application may use a unified status model, but it must not incorrectly represent a manually entered expense as a pending UPI payment.

Suggested conceptual distinction:

```text
PaymentStatus
----------------
Pending
Paid
Failed
Cancelled
Unknown

EntryType
----------------
UpiScan
Manual
```

This separation is preferred because it prevents confusion between:

```text
"Payment is pending"
```

and:

```text
"This expense was manually recorded."
```

---

# 15. Expense Entity

Use a model similar to:

```csharp
public class Expense
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpenseDateUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";

    public string? MerchantName { get; set; }

    public string? UpiId { get; set; }

    public string? Note { get; set; }

    public string? UpiTransactionNote { get; set; }

    public string? Category { get; set; }

    public string? RawUpiPayload { get; set; }

    public ExpenseEntryType EntryType { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public ExpenseStatus Status { get; set; }

    public string? PaymentReference { get; set; }
}
```

Suggested enums:

```csharp
public enum ExpenseEntryType
{
    UpiScan,
    Manual
}
```

```csharp
public enum PaymentMethod
{
    Cash,
    Upi,
    CreditCard,
    DebitCard,
    BankTransfer,
    Other
}
```

```csharp
public enum ExpenseStatus
{
    Pending,
    Paid,
    Failed,
    Cancelled,
    Unknown,
    Recorded
}
```

The exact model can evolve as implementation progresses.

---

# 16. Pending Expense Requirement

For Scan & Pay, the expense must be saved **before** launching the UPI application.

Correct:

```text
User confirms
     ↓
SQLite INSERT
Status = Pending
     ↓
Launch UPI application
```

Incorrect:

```text
Launch UPI application
     ↓
Try to save expense
```

This is critical.

If Vyaya crashes after launching PhonePe, the pending expense must still exist in SQLite.

---

# 17. UPI Deep Link

Vyaya should launch the UPI payment URI.

Example:

```text
upi://pay?pa=merchant@upi&pn=Merchant&am=450.00&cu=INR
```

Use Android intent/deep-link mechanisms.

Do not hard-code PhonePe as the only UPI application.

The user should be able to select among compatible installed UPI applications where Android allows it.

Conceptually:

```text
Vyaya
   ↓
Android UPI Intent
   ↓
PhonePe
Google Pay
Paytm
Other UPI App
```

---

# 18. Payment Result

Launching a UPI application does **not** automatically mean payment succeeded.

Possible outcomes:

```text
UPI application launched
        |
        +--> Success
        |
        +--> Failed
        |
        +--> Cancelled
        |
        +--> Unknown
```

Only mark:

```text
Paid
```

when reliable evidence is available.

Never fabricate payment success.

If the result cannot be reliably determined, use:

```text
Unknown
```

or:

```text
Pending
```

depending on the implementation.

---

# 19. Android Payment Launcher

Create an abstraction:

```csharp
public interface IUpiPaymentLauncher
{
    Task<UpiPaymentResult> LaunchAsync(
        UpiPaymentRequest request);
}
```

Keep Android-specific intent handling behind this interface.

Do not put Android intent code directly inside ViewModels.

---

# 20. Local SQLite Storage

Vyaya must be completely functional without a backend.

SQLite is the local source of truth.

The user must be able to:

- Add expenses offline.
- Scan QR codes offline.
- Save expenses offline.
- View history offline.
- View monthly reports offline.
- View charts offline.
- Export expenses offline.

For QR payments, the UPI application itself may require network connectivity to actually complete payment, but Vyaya's local expense functionality must not depend on a backend.

---

# 21. Expense History

Create an expense history screen.

Example:

```text
August 2026

23 Aug

ABC Vegetables
₹450
Groceries
Paid

Uber
₹320
Travel
Paid

Cash Lunch
₹180
Food
Recorded

Amazon
₹1,250
Shopping
Paid
```

Display:

- Date
- Merchant/description
- Amount
- Category
- Payment method
- Status

Manual expenses should be clearly distinguishable from UPI scan expenses where useful.

---

# 22. Expense Details

Tapping an expense should open a details screen.

For a UPI expense:

```text
Merchant
ABC Vegetables

Amount
₹450

Category
Groceries

Payment Method
UPI

UPI ID
abc@upi

Status
Paid

Note
Bought vegetables

Date
23 Aug 2026
```

For a manual expense:

```text
Description
Cash Lunch

Amount
₹180

Category
Food

Payment Method
Cash

Status
Recorded

Note
Lunch with friends

Date
23 Aug 2026
```

---

# 23. Monthly Expense Dashboard

Vyaya must provide monthly spending summaries.

Example:

```text
August 2026

Total Spending

₹18,450

Transactions
37

Average Expense
₹498.65
```

Category breakdown:

```text
Food          ₹5,200
Travel        ₹3,400
Groceries     ₹2,800
Shopping      ₹4,050
Bills         ₹3,000
```

The calculations must be performed locally using SQLite data.

---

# 24. What Counts Toward Spending

By default, monthly spending should include:

```text
Paid
Recorded
```

It should exclude:

```text
Pending
Failed
Cancelled
Unknown
```

However, the reporting architecture should make this rule easy to change later.

Manual expenses with:

```text
Status = Recorded
```

must count toward spending.

This is important because manual expenses represent completed expenses that Vyaya did not initiate.

---

# 25. Pie Chart

Provide a category-based pie chart.

Example:

```text
Food        30%
Travel      20%
Shopping    25%
Groceries   15%
Other       10%
```

The chart should be generated from actual local expense data.

By default:

```text
Paid + Recorded
```

expenses are included.

---

# 26. Reporting Service

Do not tightly couple chart logic to SQLite or UI.

Create a reporting abstraction.

Example:

```csharp
public interface IExpenseReportService
{
    Task<MonthlyExpenseSummary> GetMonthlySummaryAsync(
        int year,
        int month);
}
```

The service should return aggregated information such as:

```text
TotalAmount
TransactionCount
AverageAmount
CategoryTotals
```

The UI should only visualize the result.

---

# 27. Excel Export

Vyaya must support exporting expense data.

Example:

```text
[ Export Expenses ]
```

Export columns should include:

```text
Date
Merchant / Description
Amount
Currency
Category
Payment Method
Entry Type
Note
UPI ID
Payment Reference
Status
```

Example:

```text
Date        Description       Amount   Category    Method       Status
23-08-2026  ABC Store         450      Groceries   UPI          Paid
23-08-2026  Cash Lunch        180      Food        Cash         Recorded
23-08-2026  Uber              320      Travel      UPI          Paid
```

The export must work offline.

If `.xlsx` is used, use an appropriate .NET library.

The Android implementation must support saving/sharing the generated file using Android's supported storage/sharing mechanisms.

---

# 28. Privacy

Vyaya should follow a local-first privacy model.

The initial application must not require a backend.

Do not upload expense information.

Do not upload:

- Merchant names
- Amounts
- Categories
- Notes
- UPI IDs
- Payment history

The user's SQLite database is the source of truth.

---

# 29. Security

Never store:

- UPI PIN
- Bank credentials
- Card credentials
- Authentication secrets

Vyaya must never ask for a UPI PIN.

PhonePe or another UPI application handles payment authentication.

Treat QR content as untrusted input.

Do not execute arbitrary URLs obtained from QR codes.

Only support validated UPI payment URIs.

---

# 30. UPI Validation

Before launching a scanned QR payload, validate it.

Conceptually validate:

```text
Scheme = upi
Action = pay
Required fields
Amount format
Currency
```

Do not blindly execute arbitrary QR contents.

A QR scanner can encounter malicious or unrelated URLs.

---

# 31. Date Handling

The application is initially intended primarily for India.

Default currency:

```text
INR
```

Display amounts using Indian currency formatting where appropriate:

```text
₹450
₹1,250
₹18,450
```

Use `decimal` for financial amounts.

Do not use `double` or `float` for monetary calculations.

Store dates consistently and display them in the user's local timezone.

Manual expenses must allow the user to specify the expense date.

This is important because a user may be recording an expense after the actual payment occurred.

Example:

```text
Today: 23 Aug

Expense date:
[ 22 Aug 2026 ]
```

---

# 32. Manual Expense Date

Manual expense entry must allow the user to choose the date.

Example:

```text
Amount
₹500

Date
22 Aug 2026

Payment Method
Cash

Category
Groceries

Note
Weekly vegetables
```

The expense should be included in the correct month's report based on the **expense date**, not merely the date on which it was entered.

---

# 33. Camera Permission

Request camera permission only when the user starts QR scanning.

Do not request camera permission on application startup.

Example:

```text
User taps Scan & Pay
        ↓
Request Camera Permission
        ↓
Open Scanner
```

If permission is denied, provide a clear explanation and recovery path.

---

# 34. Error Handling

Handle at least:

### Invalid QR

```text
Invalid QR code.
```

### Non-UPI QR

```text
This QR code is not a supported UPI payment QR.
```

### Missing amount

Allow the user to enter the amount.

### No UPI application

```text
No compatible UPI application was found.
```

Do not mark the expense as paid.

### User cancels payment

Set:

```text
Cancelled
```

when reliably known.

### Unknown result

Set:

```text
Unknown
```

or:

```text
Pending
```

when appropriate.

### Application crash

The expense must already exist in SQLite because it was saved before launching the payment application.

---

# 35. Startup Recovery

When Vyaya starts:

```text
SQLite
   ↓
Find Pending / Unknown UPI expenses
```

Display them appropriately.

Example:

```text
Payment status needs attention

ABC Vegetables
₹450

Status: Unknown
```

Do not silently delete these expenses.

Manual expenses with `Recorded` status do not need payment reconciliation.

---

# 36. Architecture

Use MVVM and clear separation of responsibilities.

Recommended structure:

```text
Views/
    HomePage.xaml
    ScanPage.xaml
    ExpenseDetailsPage.xaml
    AddExpensePage.xaml
    ExpensesPage.xaml
    ExpenseDetailPage.xaml
    MonthlySummaryPage.xaml
    SettingsPage.xaml

ViewModels/
    HomeViewModel.cs
    ScanViewModel.cs
    ExpenseDetailsViewModel.cs
    AddExpenseViewModel.cs
    ExpensesViewModel.cs
    ExpenseDetailViewModel.cs
    MonthlySummaryViewModel.cs

Models/
    Expense.cs
    Category.cs
    UpiPaymentRequest.cs
    UpiPaymentResult.cs

Services/
    IUpiParser.cs
    IUpiPaymentLauncher.cs
    IExpenseService.cs
    ICategoryService.cs
    IExpenseReportService.cs
    IExportService.cs

Data/
    AppDatabase.cs
    ExpenseRepository.cs
    CategoryRepository.cs

Platforms/
    Android/
        ...
```

The exact structure can evolve.

Responsibilities must remain separated.

---

# 37. Application Architecture

Conceptually:

```text
                    ┌───────────────────┐
                    │      Vyaya UI     │
                    └─────────┬─────────┘
                              │
                    ┌─────────▼─────────┐
                    │     ViewModels    │
                    └─────────┬─────────┘
                              │
                    ┌─────────▼─────────┐
                    │ Application Logic │
                    └─────────┬─────────┘
                              │
             ┌────────────────┼─────────────────┐
             │                │                 │
             ▼                ▼                 ▼
          SQLite          UPI Parser        Reporting
             │                │
             │                ▼
             │        Android UPI Intent
             │                │
             │                ▼
             │        PhonePe / GPay /
             │        Paytm / Other
             │
             ▼
       Local Expense Data
```

---

# 38. Dependency Injection

Use .NET MAUI's built-in dependency injection.

Register services such as:

```text
IUpiParser
IUpiPaymentLauncher
IExpenseService
IExpenseRepository
ICategoryService
IExpenseReportService
IExportService
```

Avoid service locator patterns.

Avoid global static state.

---

# 39. MVVM Rules

ViewModels contain application/presentation logic.

Views primarily contain UI.

Do not put:

- SQLite queries
- UPI parsing
- Android intents
- Financial calculations

directly into code-behind.

Avoid:

```csharp
private void PayButton_Clicked(...)
{
    // Parse QR
    // Insert SQLite
    // Launch Android Intent
}
```

Prefer:

```text
View
 ↓
ViewModel
 ↓
Service
 ↓
Repository / Platform Service
```

---

# 40. Database

Use SQLite.

The schema must be designed so it can evolve.

Consider indexes for:

```text
ExpenseDate
Category
Status
PaymentMethod
MerchantName
```

Do not prematurely optimize.

Prioritize correctness and simplicity.

---

# 41. Testing

Create unit tests for important business logic.

## UPI Parser

Test:

- Valid UPI URI
- Missing amount
- Missing payee name
- URL-encoded values
- Optional parameters
- Invalid URI
- Non-UPI URI

Example:

```text
upi://pay?pa=test@upi&pn=Test&am=100&cu=INR
```

Expected:

```text
PaymentAddress = test@upi
PayeeName = Test
Amount = 100
Currency = INR
```

## Expense Calculations

Test:

- Monthly total
- Category totals
- Paid expenses
- Recorded manual expenses
- Pending excluded
- Failed excluded
- Cancelled excluded
- Unknown excluded
- Empty month
- Multiple categories
- Manual expense on previous date/month

## Category Search

Test:

```text
"trav"
```

returns:

```text
Travel
```

## Manual Expense

Test:

```text
Amount
PaymentMethod
Category
Note
ExpenseDate
```

are correctly persisted.

---

# 42. UX Principles

Vyaya should be extremely fast for common expenses.

For QR payment:

```text
Scan
 ↓
Choose category
 ↓
Optional note
 ↓
Pay
```

For manual expense:

```text
Add Expense
 ↓
Amount
 ↓
Payment method
 ↓
Category
 ↓
Optional note
 ↓
Save
```

Avoid unnecessary screens.

A user should be able to record a cash expense in seconds.

---

# 43. Home Dashboard

The Home screen should provide an overview.

Example:

```text
Vyaya

August 2026

₹18,450
Total spending

37 expenses

[ Scan & Pay ]
[ Add Expense ]

This Month
-----------------
Food          ₹5,200
Travel        ₹3,400
Groceries     ₹2,800

Recent
-----------------
ABC Store     ₹450
Cash Lunch    ₹180
Uber          ₹320
```

---

# 44. Future Extensibility

The architecture should leave room for future features such as:

- Custom categories
- Budgets
- Recurring expenses
- Multiple currencies
- Search
- Advanced reports
- Cloud backup
- Device synchronization
- Importing bank statements
- Receipt scanning
- AI-assisted categorization

However, these are **not part of the initial implementation**.

Do not build them unless explicitly requested.

---

# 45. Do Not Build Yet

For the initial version, do NOT introduce:

- Backend server
- User accounts
- Cloud synchronization
- Authentication
- Push notifications
- Social features
- AI categorization
- Bank account integration
- Automatic bank statement synchronization
- Payment processing
- Complex budgeting
- Subscription system

The first milestone is:

**A robust local Android expense tracker with both UPI QR-based payment recording and manual expense recording.**

---

# 46. Development Phases

## Phase 1 — Project Foundation

Create:

- .NET 10 MAUI project
- Android target
- MVVM structure
- Dependency injection
- SQLite
- Basic navigation
- Expense model
- Category model

Verify Android build and deployment.

---

## Phase 2 — Local Expense Database

Implement:

- Expense entity
- Category entity
- Payment method
- Entry type
- Expense status
- SQLite schema
- Repository
- Expense service
- CRUD operations

---

## Phase 3 — Manual Expense Entry

Implement:

- Add Expense screen
- Amount
- Date
- Payment method
- Category search
- Note
- Save operation

Verify that manual expenses appear in history.

This phase should work completely offline.

---

## Phase 4 — QR Scanner

Implement:

- Camera permission
- QR scanner
- Payload decoding
- UPI URI validation

---

## Phase 5 — UPI Parser

Implement:

```text
UPI payload
     ↓
UpiPaymentRequest
```

Add comprehensive parser tests.

---

## Phase 6 — Scan Expense Entry

Implement:

- Merchant display
- Amount
- UPI ID
- Category
- Note
- Pending status
- SQLite persistence

---

## Phase 7 — UPI Launch

Implement Android UPI intent/deep-link launching.

Required sequence:

```text
Save Pending Expense
        ↓
Launch UPI application
```

---

## Phase 8 — Payment Result

Investigate and implement the most reliable Android/UPI result mechanism available.

Support:

```text
Paid
Failed
Cancelled
Unknown/Pending
```

Do not assume that a returned intent proves payment success unless the available result is trustworthy.

---

## Phase 9 — Expense History

Implement:

- Expense list
- Date grouping
- Category
- Payment method
- Status
- Entry type
- Expense details

---

## Phase 10 — Monthly Dashboard

Implement:

- Monthly total
- Transaction count
- Average expense
- Category totals
- Previous/next month

---

## Phase 11 — Pie Chart

Implement category-based spending visualization.

Default included statuses:

```text
Paid
Recorded
```

---

## Phase 12 — Excel Export

Implement:

- Export all expenses
- Export selected month
- `.xlsx` or appropriate Excel-compatible format
- Android save/share flow

---

## Phase 13 — UX Polish

Improve:

- Navigation
- Empty states
- Error messages
- Loading states
- Accessibility
- Android UX
- Visual consistency
- Scan experience
- Manual-entry experience

---

# 47. Definition of Done

The initial POC is successful when a real Android device can perform both flows.

## UPI Flow

```text
1. Open Vyaya
2. Tap Scan & Pay
3. Scan a real UPI QR
4. Decode QR
5. Recognize UPI payment URI
6. Extract merchant and amount
7. Search/select category
8. Add optional note
9. Save expense as Pending
10. Launch Android UPI intent
11. Select PhonePe or another UPI application
12. Complete/cancel payment
13. Return to Vyaya
14. Update status when reliable
15. See expense in history
16. See it in monthly total
17. See it in category chart
18. Export it
```

## Manual Flow

```text
1. Open Vyaya
2. Tap Add Expense
3. Enter amount
4. Select expense date
5. Select payment method
6. Search/select category
7. Add optional note
8. Save expense
9. See expense in history
10. See expense in monthly total
11. See expense in category chart
12. Export expense
```

Both flows must work without a backend.

---

# 48. Development Priority

Implement features in this order:

```text
1. .NET 10 MAUI foundation
2. SQLite
3. Expense model
4. Category model
5. Manual expense entry
6. Expense history
7. QR scanning
8. UPI parsing
9. UPI expense entry
10. Pending expense persistence
11. Android UPI intent
12. Payment result handling
13. Monthly summary
14. Category aggregation
15. Pie chart
16. Excel export
17. UX polish
```

Do not start with visual polish before the core expense lifecycle works.

---

# 49. Agent Instructions

When working on Vyaya:

1. Read this `AGENTS.md` before making architectural changes.
2. Preserve the local-first architecture.
3. Do not introduce a backend unless explicitly requested.
4. Do not make PhonePe a hard dependency.
5. Keep UPI payment launching behind an abstraction.
6. Never claim payment success without reliable evidence.
7. Save UPI expenses before launching the UPI application.
8. Keep manual expenses independent of UPI.
9. Support cash, card, bank transfer, UPI, and other payment methods.
10. Keep financial calculations precise.
11. Use `decimal` for monetary values.
12. Keep Android-specific code isolated.
13. Write tests for UPI parsing and expense calculations.
14. Write tests for manual expense recording.
15. Prefer incremental implementation over large rewrites.
16. After each major feature, ensure the project still builds.
17. Do not add unnecessary dependencies.
18. Explain significant architectural decisions before making large structural changes.
19. If Android/UPI behavior depends on external platform behavior, verify the relevant documentation instead of guessing.
20. Never store UPI PINs, bank credentials, or card credentials.
21. Treat QR payloads as untrusted input.
22. Do not execute arbitrary URLs from QR codes.
23. Preserve pending/unknown UPI expenses rather than deleting them.
24. Keep reporting logic independent from the UI.
25. Ensure manual expenses are included in reporting as completed/recorded expenses.

---

# 50. First Task for the Coding Agent

Start by creating the .NET 10 MAUI Android project foundation.

Implement only:

```text
.NET 10 MAUI
    |
    +── MVVM
    |
    +── SQLite
    |
    +── Expense model
    |
    +── Category model
    |
    +── PaymentMethod
    |
    +── ExpenseEntryType
    |
    +── ExpenseStatus
    |
    +── Repository
    |
    +── Expense service
    |
    +── Basic Home screen
    |
    +── Basic Add Expense screen
    |
    +── Basic Expense History screen
```

Do **not** implement QR scanning or UPI launching in the first task.

First establish a clean, buildable foundation.

Then implement the features incrementally according to the development phases above.

---

# Product Identity

Application name:

**Vyaya**

Sanskrit:

**व्यय**

Meaning:

**Expenditure / spending / expense**

The core product concept is:

```text
Vyaya

Record every expense.

Scan when you pay by UPI.
Add manually when you don't.
Understand where your money goes.
```

The application should remain a **simple, fast, private, local-first expense ledger**, with QR-based UPI payment integration as its signature feature rather than its only method of recording expenses.