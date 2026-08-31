using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Controls;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public class ChartBasisOptionItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public partial class MonthlySummaryViewModel : BaseViewModel
{
    private readonly IExpenseReportService _reportService;
    private readonly IExpenseService _expenseService;
    private readonly IExportService _exportService;

    private MonthlyExpenseSummary? _currentSummary;
    private List<Expense> _currentExpenses = new();

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private string _monthDisplayTitle = string.Empty;

    [ObservableProperty]
    private string _totalSpendingFormatted = "₹0";

    [ObservableProperty]
    private decimal _totalSpendingRaw;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private string _averageExpenseFormatted = "₹0";

    [ObservableProperty]
    private bool _hasNoData;

    [ObservableProperty]
    private string _selectedChartBasis = "Category";

    [ObservableProperty]
    private bool _isBasisModalOpen;

    [ObservableProperty]
    private DonutChartDrawable _chartDrawable = new();

    public ObservableCollection<ChartBasisOptionItem> BasisOptions { get; } = new();
    public ObservableCollection<ChartSegment> CurrentChartSegments { get; } = new();
    public ObservableCollection<CategoryExpenseSummary> CategoryBreakdown { get; } = new();
    public ObservableCollection<PaymentMethodExpenseSummary> PaymentMethodBreakdown { get; } = new();

    public MonthlySummaryViewModel(
        IExpenseReportService reportService,
        IExpenseService expenseService,
        IExportService exportService)
    {
        _reportService = reportService;
        _expenseService = expenseService;
        _exportService = exportService;
        Title = "Analytics";

        var now = DateTime.Now;
        SelectedYear = now.Year;
        SelectedMonth = now.Month;

        RefreshBasisOptions();
    }

    partial void OnSelectedChartBasisChanged(string value)
    {
        RefreshBasisOptions();
        RefreshChart();
    }

    private void RefreshBasisOptions()
    {
        BasisOptions.Clear();
        BasisOptions.Add(new ChartBasisOptionItem
        {
            Id = "Category",
            Title = "By Category",
            Subtitle = "Food, Groceries, Travel, Shopping, Bills...",
            Icon = "🍔",
            IsSelected = SelectedChartBasis == "Category"
        });

        BasisOptions.Add(new ChartBasisOptionItem
        {
            Id = "Payment Method",
            Title = "By Payment Method",
            Subtitle = "UPI, Cash, Credit Card, Debit Card, Bank Transfer",
            Icon = "💳",
            IsSelected = SelectedChartBasis == "Payment Method"
        });

        // BasisOptions.Add(new ChartBasisOptionItem
        // {
        //     Id = "Entry Type",
        //     Title = "By Entry Mode",
        //     Subtitle = "Scan & Pay QR vs Add Manually",
        //     Icon = "⚡",
        //     IsSelected = SelectedChartBasis == "Entry Type"
        // });
    }

    [RelayCommand]
    public void OpenBasisModal()
    {
        RefreshBasisOptions();
        IsBasisModalOpen = true;
    }

    [RelayCommand]
    public void CloseBasisModal()
    {
        IsBasisModalOpen = false;
    }

    [RelayCommand]
    public void SelectBasis(string basisId)
    {
        SelectedChartBasis = basisId;
        IsBasisModalOpen = false;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch { }
    }

    [RelayCommand]
    public async Task LoadSummaryAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearError();

        try
        {
            _currentSummary = await _reportService.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
            _currentExpenses = await _expenseService.GetExpensesByMonthAsync(SelectedYear, SelectedMonth);

            MonthDisplayTitle = _currentSummary.MonthName;
            TotalSpendingFormatted = _currentSummary.FormattedTotalAmount;
            TotalSpendingRaw = _currentSummary.TotalAmount;
            TotalTransactions = _currentSummary.TransactionCount;
            AverageExpenseFormatted = _currentSummary.FormattedAverageAmount;
            HasNoData = _currentSummary.TransactionCount == 0;

            CategoryBreakdown.Clear();
            foreach (var cat in _currentSummary.CategoryTotals)
            {
                CategoryBreakdown.Add(cat);
            }

            PaymentMethodBreakdown.Clear();
            foreach (var method in _currentSummary.PaymentMethodTotals)
            {
                PaymentMethodBreakdown.Add(method);
            }

            RefreshChart();
        }
        catch (Exception ex)
        {
            SetError($"Error loading summary: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshChart()
    {
        if (_currentSummary == null)
            return;

        var segments = new List<ChartSegment>();

        if (SelectedChartBasis == "Category")
        {
            // Category Breakdown (Default)
            foreach (var cat in _currentSummary.CategoryTotals)
            {
                segments.Add(new ChartSegment
                {
                    Name = cat.CategoryName,
                    TotalAmount = cat.TotalAmount,
                    Percentage = cat.Percentage,
                    Count = cat.Count,
                    Icon = cat.Icon,
                    ColorHex = cat.ColorHex
                });
            }
        }
        else if (SelectedChartBasis == "Payment Method")
        {
            // Payment Method Breakdown
            var methodColors = new Dictionary<PaymentMethod, string>
            {
                { PaymentMethod.Cash, "#34C759" },
                { PaymentMethod.Upi, "#5856D6" },
                { PaymentMethod.CreditCard, "#FF9500" },
                { PaymentMethod.DebitCard, "#0A84FF" },
                { PaymentMethod.BankTransfer, "#30B0C7" },
                { PaymentMethod.Other, "#8E8E93" }
            };

            foreach (var method in _currentSummary.PaymentMethodTotals)
            {
                var color = methodColors.TryGetValue(method.PaymentMethod, out var c) ? c : "#5856D6";
                segments.Add(new ChartSegment
                {
                    Name = method.Name,
                    TotalAmount = method.TotalAmount,
                    Percentage = method.Percentage,
                    Count = method.Count,
                    Icon = method.Icon,
                    ColorHex = color
                });
            }
        }
        // else if (SelectedChartBasis == "Entry Type")
        // {
        //     // Entry Type Breakdown (Scan & Pay vs Manual)
        //     var validExpenses = _currentExpenses
        //         .Where(e => e.Status == ExpenseStatus.Paid || e.Status == ExpenseStatus.Recorded)
        //         .ToList();

        //     var upiScanAmount = validExpenses.Where(e => e.EntryType == ExpenseEntryType.UpiScan).Sum(e => e.Amount);
        //     var manualAmount = validExpenses.Where(e => e.EntryType == ExpenseEntryType.Manual).Sum(e => e.Amount);
        //     var total = upiScanAmount + manualAmount;

        //     if (upiScanAmount > 0)
        //     {
        //         var pct = total > 0 ? (double)(upiScanAmount / total * 100) : 0;
        //         segments.Add(new ChartSegment
        //         {
        //             Name = "Scan & Pay",
        //             TotalAmount = upiScanAmount,
        //             Percentage = pct,
        //             Count = validExpenses.Count(e => e.EntryType == ExpenseEntryType.UpiScan),
        //             Icon = "📷",
        //             ColorHex = "#0A84FF"
        //         });
        //     }

        //     if (manualAmount > 0)
        //     {
        //         var pct = total > 0 ? (double)(manualAmount / total * 100) : 0;
        //         segments.Add(new ChartSegment
        //         {
        //             Name = "Add Manually",
        //             TotalAmount = manualAmount,
        //             Percentage = pct,
        //             Count = validExpenses.Count(e => e.EntryType == ExpenseEntryType.Manual),
        //             Icon = "✍️",
        //             ColorHex = "#34C759"
        //         });
        //     }
        // }

        CurrentChartSegments.Clear();
        foreach (var seg in segments)
        {
            CurrentChartSegments.Add(seg);
        }

        // Recreate Donut Drawable
        ChartDrawable = new DonutChartDrawable
        {
            Segments = segments,
            TotalAmount = _currentSummary.TotalAmount,
            CenterText = _currentSummary.FormattedTotalAmount,
            CenterSubtext = SelectedChartBasis == "Category" ? "Total Spending" : $"{SelectedChartBasis}"
        };
    }

    [RelayCommand]
    public async Task PreviousMonthAsync()
    {
        var currentDate = new DateTime(SelectedYear, SelectedMonth, 1).AddMonths(-1);
        SelectedYear = currentDate.Year;
        SelectedMonth = currentDate.Month;
        await LoadSummaryAsync();
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        var currentDate = new DateTime(SelectedYear, SelectedMonth, 1).AddMonths(1);
        SelectedYear = currentDate.Year;
        SelectedMonth = currentDate.Month;
        await LoadSummaryAsync();
    }

    [RelayCommand]
    public async Task ExportMonthAsync()
    {
        IsBusy = true;
        try
        {
            var expenses = await _expenseService.GetExpensesByMonthAsync(SelectedYear, SelectedMonth);
            if (expenses.Count == 0)
            {
                SetError("No expenses found for this month to export.");
                return;
            }

            var monthStr = new DateTime(SelectedYear, SelectedMonth, 1).ToString("yyyy_MM");
            var filePath = await _exportService.ExportExpensesToFileAsync(expenses, $"Vyaya_Expenses_{monthStr}");
            await _exportService.ShareExportedFileAsync(filePath, $"Export {MonthDisplayTitle} Expenses");
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
