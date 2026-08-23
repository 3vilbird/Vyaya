using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Controls;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class MonthlySummaryViewModel : BaseViewModel
{
    private readonly IExpenseReportService _reportService;
    private readonly IExpenseService _expenseService;
    private readonly IExportService _exportService;

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
    private DonutChartDrawable _chartDrawable = new();

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
            var summary = await _reportService.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);

            MonthDisplayTitle = summary.MonthName;
            TotalSpendingFormatted = summary.FormattedTotalAmount;
            TotalSpendingRaw = summary.TotalAmount;
            TotalTransactions = summary.TransactionCount;
            AverageExpenseFormatted = summary.FormattedAverageAmount;
            HasNoData = summary.TransactionCount == 0;

            CategoryBreakdown.Clear();
            foreach (var cat in summary.CategoryTotals)
            {
                CategoryBreakdown.Add(cat);
            }

            PaymentMethodBreakdown.Clear();
            foreach (var method in summary.PaymentMethodTotals)
            {
                PaymentMethodBreakdown.Add(method);
            }

            // Update Donut Chart
            var newDrawable = new DonutChartDrawable
            {
                Segments = summary.CategoryTotals,
                TotalAmount = summary.TotalAmount,
                CenterText = summary.FormattedTotalAmount,
                CenterSubtext = "Total Spending"
            };
            ChartDrawable = newDrawable;
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
