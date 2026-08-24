# Vyaya (व्यय) — Local-First Expense Tracker

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-Android-0A84FF?logo=android&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
[![License: MIT](https://img.shields.io/badge/License-MIT-34C759.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-28%20Passed-34C759.svg)](tests/Vyaya.Tests)

**Vyaya (व्यय)** is a Sanskrit-derived word meaning **expenditure, spending, or expense**.

Vyaya is a modern, **local-first personal expense tracker** for Android built with **.NET 10 MAUI**, **C#**, and **SQLite**. It offers an elegant, iOS-grade aesthetic and user experience. Its primary signature differentiator is allowing users to instantly record an expense by scanning a UPI QR code and continuing the payment through PhonePe, Google Pay, Paytm, or another UPI application—while remaining **100% independent of any specific payment gateway or backend server**.

---

## 🌟 Product Philosophy

1. **Expense Tracker First, Not a Payment Processor**:
   - Vyaya **never** processes payments itself.
   - Vyaya **never** handles UPI PINs, passwords, or bank credentials.
   - All payment authentications happen securely inside your chosen banking or UPI application (PhonePe, GPay, etc.).
2. **100% Local-First & Private**:
   - **Zero backend servers**: All transactions, categories, notes, and metrics reside purely inside an on-device SQLite database.
   - **Zero telemetry or data tracking**: Your financial privacy is completely preserved offline.
3. **Dual Expense-Entry Flows**:
   - **Mode A (Scan & Pay)**: Scan any UPI QR code, verify amount and category, save as `Pending` in SQLite, launch UPI app via Android intent chooser, and reconcile status.
   - **Mode B (Manual Entry)**: First-class manual expense recording for Cash, Cards, Bank Transfers, and offline payments.

---

## 🏗️ Architecture Overview

Vyaya is engineered using the **MVVM (Model-View-ViewModel)** pattern with .NET MAUI's built-in **Dependency Injection** system, ensuring a decoupled separation of concerns.

```text
                               ┌──────────────────────────┐
                               │         Vyaya UI         │
                               │ (XAML Pages & Controls)  │
                               └────────────┬─────────────┘
                                            │
                               ┌────────────▼─────────────┐
                               │   CommunityToolkit MVVM  │
                               │       (ViewModels)       │
                               └────────────┬─────────────┘
                                            │
                               ┌────────────▼─────────────┐
                               │   Domain Services Layer  │
                               └────────────┬─────────────┘
                                            │
             ┌──────────────────────────────┼──────────────────────────────┐
             │                              │                              │
             ▼                              ▼                              ▼
  ┌────────────────────┐         ┌────────────────────┐         ┌────────────────────┐
  │  SQLite Database   │         │     UPI Parser     │         │ Reporting & Export │
  │  (Local AppData)   │         │ (PhonePe/UPI/VPA)  │         │ (Donut / CSV / XL) │
  └────────────────────┘         └──────────┬─────────┘         └────────────────────┘
                                            │
                                 ┌──────────▼─────────┐
                                 │ Android UPI Intent │
                                 │   Chooser Launcher │
                                 └──────────┬─────────┘
                                            │
                             ┌──────────────┼──────────────┐
                             ▼              ▼              ▼
                         PhonePe        Google Pay       Paytm / Other
```

---

## 📂 Project Structure

```text
expense-tracker/
├── AGENTS.md                                # Full product architecture specification
├── README.md                                # Setup, build, and architecture documentation
├── .gitignore                               # Git ignore configuration
├── Vyaya.sln                                # Solution file
├── src/
│   └── Vyaya/                               # .NET 10 MAUI Application Project
│       ├── Controls/
│       │   └── DonutChartDrawable.cs        # Custom Microsoft.Maui.Graphics 2D Donut Chart
│       ├── Converters/
│       │   └── Converters.cs                # Value converters (InvertedBool, NullOrEmpty, etc.)
│       ├── Data/
│       │   ├── AppDatabase.cs               # SQLiteAsyncConnection & schema manager
│       │   ├── IExpenseRepository.cs        # Expense data access contract
│       │   ├── ExpenseRepository.cs         # SQLite CRUD & date-range queries
│       │   ├── ICategoryRepository.cs       # Category data access contract
│       │   └── CategoryRepository.cs        # Category search & seed data
│       ├── Models/
│       │   ├── Enums.cs                     # ExpenseEntryType, PaymentMethod, ExpenseStatus
│       │   ├── Expense.cs                   # SQLite Expense entity with formatting helpers
│       │   ├── Category.cs                  # Category entity with icons and colors
│       │   ├── UpiPaymentRequest.cs         # Parsed UPI data model & URI builder
│       │   ├── UpiPaymentResult.cs          # Payment execution result wrapper
│       │   └── MonthlyExpenseSummary.cs     # Aggregated spending & category totals
│       ├── Platforms/
│       │   └── Android/
│       │       └── AndroidManifest.xml      # Camera permissions & UPI intent queries
│       ├── Resources/
│       │   └── Styles/
│       │       ├── Colors.xaml              # Apple iOS-style vibrant color palette
│       │       └── Styles.xaml              # System controls, typography & elevation styles
│       ├── Services/
│       │   ├── IUpiParser.cs & UpiParser.cs                 # Multi-format UPI URI & VPA parser
│       │   ├── IUpiPaymentLauncher.cs & UpiPaymentLauncher.cs # Android Intent chooser launcher
│       │   ├── IExpenseService.cs & ExpenseService.cs       # Business logic & pending persistence
│       │   ├── ICategoryService.cs & CategoryService.cs     # Category management & search
│       │   ├── IExpenseReportService.cs & ExpenseReportService.cs # Pure monthly calculations
│       │   └── IExportService.cs & ExportService.cs         # Offline RFC-4180 CSV / Excel export
│       ├── ViewModels/
│       │   ├── BaseViewModel.cs             # ObservableObject base with error handling
│       │   ├── HomeViewModel.cs             # Dashboard hero metrics, attention banner & recents
│       │   ├── ScanViewModel.cs             # Live camera scanning & clipboard paste
│       │   ├── ExpenseDetailsViewModel.cs   # Scan review & "Pay with UPI" execution
│       │   ├── AddExpenseViewModel.cs       # Manual expense entry with quick keypad
│       │   ├── ExpensesViewModel.cs         # Date-grouped history with multi-filter search
│       │   ├── ExpenseDetailViewModel.cs     # Digital receipt view & status reconciliation
│       │   ├── MonthlySummaryViewModel.cs   # Analytics, donut chart & month switcher
│       │   └── SettingsViewModel.cs         # Local database stats & export manager
│       ├── Views/
│       │   ├── HomePage.xaml (.cs)          # Main Dashboard
│       │   ├── ScanPage.xaml (.cs)          # Camera QR Scanner (ZXing.Net.Maui)
│       │   ├── ExpenseDetailsPage.xaml (.cs)# Payment Confirmation Page
│       │   ├── AddExpensePage.xaml (.cs)    # Manual Expense Form
│       │   ├── ExpensesPage.xaml (.cs)      # Grouped Expense History
│       │   ├── ExpenseDetailPage.xaml (.cs) # Digital Receipt View
│       │   ├── MonthlySummaryPage.xaml (.cs)# Analytics & Donut Chart
│       │   └── SettingsPage.xaml (.cs)      # Settings & Export View
│       ├── AppShell.xaml (.cs)              # iOS-styled 4-tab bottom navigation bar
│       ├── MauiProgram.cs                   # Dependency injection container & bootstrapping
│       └── Vyaya.csproj                     # Multi-targeted .NET 10 project file
└── tests/
    └── Vyaya.Tests/                         # xUnit Test Suite (.NET 10)
        ├── Services/
        │   ├── UpiParserTests.cs            # Tests standard, PhonePe, Intent & VPA formats
        │   ├── ExpenseReportServiceTests.cs # Tests inclusion/exclusion rules & math precision
        │   ├── CategorySearchTests.cs       # Tests keyword search & case insensitivity
        │   ├── ExpenseServiceTests.cs       # Tests pending persistence order & statuses
        │   └── ExportServiceTests.cs        # Tests CSV escaping & header compliance
        └── Vyaya.Tests.csproj               # xUnit test project referencing Vyaya
```

---

## ⚡ Key Features

### 1. Mode A: Scan & Pay (UPI QR Code)
- **Live Camera Scanner**: Hardware-accelerated camera scanning using `ZXing.Net.Maui.Controls` with `TryHarder = true` (reads PhonePe QRs with center logos) and `TryInverted = true` (reads screens & dark modes).
- **Multi-Format UPI Parsing**: Supports `upi://pay`, `phonepe://pay`, `intent://pay`, BharatQR / EMVCo, and raw VPAs (`merchant@ybl`).
- **Crash-Proof Pending Persistence**: Crucial architectural guarantee—Vyaya **saves the transaction as `Pending` in SQLite BEFORE launching the UPI app**. If Android terminates the app during payment, your record is never lost.
- **Android Intent Chooser**: Deep-links to PhonePe, Google Pay, Paytm, BHIM, or Cred without vendor lock-in.

### 2. Mode B: Manual Expense Entry
- First-class support for Cash, UPI, Credit Card, Debit Card, Bank Transfer, and Other payment methods.
- Quick amount increment buttons (`+₹100`, `+₹500`, `+₹1000`).
- Date picker allowing past/historical expense recording.
- Real-time searchable category picker with keyword matching.
- Saves directly with `Status = Recorded` and `EntryType = Manual`.

### 3. Spending Analytics & Apple-Style Donut Chart
- Hardware-accelerated 2D Donut Chart drawn using `Microsoft.Maui.Graphics.IDrawable`.
- Month-over-month navigation (`< August 2026 >`).
- Section 24 Spending Rules: Aggregates `Paid` and `Recorded` expenses while strictly excluding `Pending`, `Failed`, `Cancelled`, and `Unknown` transactions.
- Category progress bars and payment method distribution percentages.

### 4. Date-Grouped History & Digital Receipts
- Grouped by Date (e.g. *Today • 23 Aug*, *Yesterday*, *15 Aug 2026*) with calculated daily totals.
- Instant search bar and filtering chips (by Category, Method, or Status).
- Apple Wallet style digital receipts with full audit metadata.
- One-tap status reconciliation for unresolved UPI transactions (*"Mark as Paid"*, *"Mark as Cancelled"*).

### 5. Offline RFC-4180 CSV / Excel Export
- Generates compliant CSV / Excel files with headers: `Date,Merchant / Description,Amount,Currency,Category,Payment Method,Entry Type,Note,UPI ID,Payment Reference,Status`.
- Opens Android native share sheet to save or send via WhatsApp, Drive, Files, or Email.

---

## 📋 Prerequisites

To build and run Vyaya from source, ensure you have installed:

1. **.NET 10 SDK** (Version 10.0.100 or later):
   ```bash
   dotnet --version
   ```
2. **.NET MAUI Workloads**:
   ```bash
   dotnet workload install maui-android
   ```
3. **Android SDK & OpenJDK**:
   - Android SDK API Level 23 or higher (Target: API 35/36).
   - Java Development Kit (OpenJDK 17 or 21).
   - Set the `ANDROID_HOME` environment variable (e.g., `export ANDROID_HOME=$HOME/Android/Sdk`).

---

## 🛠️ Step-by-Step Build & Setup Guide

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/vyaya.git
cd vyaya
```

### 2. Restore NuGet Dependencies
```bash
dotnet restore
```

### 3. Run the Unit Test Suite
Execute the xUnit test suite covering UPI parsing, reporting calculations, status exclusions, and repositories:
```bash
dotnet test tests/Vyaya.Tests/Vyaya.Tests.csproj
```
*Expected Output:*
```text
Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28
```

### 4. Build the Android Application
To build the Android Debug package:
```bash
dotnet build src/Vyaya/Vyaya.csproj -f net10.0-android
```

To build an optimized Release APK:
```bash
dotnet build src/Vyaya/Vyaya.csproj -f net10.0-android -c Release
```
The resulting APK will be placed in:
`src/Vyaya/bin/Release/net10.0-android/publish/`

### 5. Run on an Android Device or Emulator
Ensure your physical Android device has **USB Debugging** enabled, or start an Android emulator:
```bash
# Check connected devices
adb devices

# Build, deploy, and launch Vyaya
dotnet build src/Vyaya/Vyaya.csproj -t:Run -f net10.0-android
```
dotnet build -t:Run -f net10.0-android

---

## 📊 Domain Models & Enums

### Enums ([`Enums.cs`](file:///home/ganaa/Desktop/expense-tracker/src/Vyaya/Models/Enums.cs))
```csharp
public enum ExpenseEntryType
{
    UpiScan = 0,   // Initiated via UPI QR scan
    Manual = 1     // Entered manually
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
    Pending = 0,   // UPI intent launched; awaiting completion
    Paid = 1,      // Verified / confirmed payment
    Failed = 2,    // Payment failed
    Cancelled = 3, // Payment cancelled by user
    Unknown = 4,   // Payment outcome unverified
    Recorded = 5   // Completed manual expense
}
```

### Default Seed Categories ([`AppDatabase.cs`](file:///home/ganaa/Desktop/expense-tracker/src/Vyaya/Data/AppDatabase.cs))
| Category | Icon | Color Hex | Sample Keywords |
| :--- | :---: | :--- | :--- |
| **Food** | 🍔 | `#FF9500` | lunch, dinner, breakfast, snacks |
| **Vegetables** | 🥦 | `#34C759` | veggies, sabzi, fruit, mandi |
| **Groceries** | 🛒 | `#30B0C7` | supermarket, milk, bread, ration |
| **Restaurants**| 🍽️ | `#FF2D55` | dining, cafe, zomato, swiggy |
| **Travel** | ✈️ | `#007AFF` | flight, train, uber, ola, metro |
| **Fuel** | ⛽ | `#FF9500` | petrol, diesel, cng, gas |
| **Shopping** | 🛍️ | `#AF52DE` | clothes, amazon, flipkart, mall |
| **Bills** | 🧾 | `#5856D6` | recharge, mobile, dth, wifi |
| **Utilities** | 💡 | `#FFCC00` | electricity, water, gas, maintenance |
| **Entertainment** | 🎬 | `#FF3B30` | movies, cinema, netflix, prime |
| **Health** | 💊 | `#34C759` | medicine, doctor, pharmacy |
| **Education** | 📚 | `#5856D6` | books, tuition, school, college |
| **Rent** | 🏠 | `#8E8E93` | house, pg, flat, lease |
| **Subscriptions**| 📱 | `#007AFF` | icloud, spotify, youtube, software |
| **Other** | 📦 | `#8E8E93` | miscellaneous, general |

---

## 🔒 Security & Privacy

- **Untrusted Input Sanitation**: Scanned QR codes are treated as untrusted data. Only validated UPI URI schemes and VPA patterns are executed.
- **Zero Financial Credentials**: Vyaya never requests or stores UPI PINs, CVVs, card numbers, or bank login details.
- **Local Isolation**: No analytics SDKs, advertising IDs, or background sync tasks are present.

---

## 📜 License

Vyaya is open-source software licensed under the [MIT License](LICENSE).
