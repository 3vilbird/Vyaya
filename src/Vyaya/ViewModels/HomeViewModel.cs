using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IExpenseService _expenseService;
    private readonly IExpenseReportService _reportService;

    [ObservableProperty]
    private string _monthlyTotal = "₹0";

    [ObservableProperty]
    private string _currentMonthName = string.Empty;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private string _averageExpense = "₹0";

    [ObservableProperty]
    private bool _hasPendingAttention;

    [ObservableProperty]
    private int _pendingAttentionCount;

    [ObservableProperty]
    private bool _isRefreshing;

    public ObservableCollection<Expense> RecentExpenses { get; } = new();
    public ObservableCollection<CategoryExpenseSummary> TopCategories { get; } = new();
    public ObservableCollection<Expense> PendingAttentionExpenses { get; } = new();

    public HomeViewModel(IExpenseService expenseService, IExpenseReportService reportService)
    {
        _expenseService = expenseService;
        _reportService = reportService;
        Title = "Vyaya";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearError();

        try
        {
            var now = DateTime.Now;
            var summary = await _reportService.GetMonthlySummaryAsync(now.Year, now.Month);

            CurrentMonthName = summary.MonthName;
            MonthlyTotal = summary.FormattedTotalAmount;
            TransactionCount = summary.TransactionCount;
            AverageExpense = summary.FormattedAverageAmount;

            // Load Top Categories
            TopCategories.Clear();
            foreach (var cat in summary.CategoryTotals.Take(4))
            {
                TopCategories.Add(cat);
            }

            // Load Recent Expenses
            var recents = await _expenseService.GetRecentExpensesAsync(8);
            RecentExpenses.Clear();
            foreach (var exp in recents)
            {
                RecentExpenses.Add(exp);
            }

            // Load Pending Attention
            var pending = await _expenseService.GetPendingAttentionExpensesAsync();
            PendingAttentionExpenses.Clear();
            foreach (var p in pending)
            {
                PendingAttentionExpenses.Add(p);
            }

            PendingAttentionCount = pending.Count;
            HasPendingAttention = PendingAttentionCount > 0;
        }
        catch (Exception ex)
        {
            SetError($"Error loading data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task ScanAndPayAsync()
    {
        await Shell.Current.GoToAsync("///scan");
    }

    [RelayCommand]
    public async Task AddExpenseAsync()
    {
        await Shell.Current.GoToAsync("addExpense");
    }

    [RelayCommand]
    public async Task ViewAllExpensesAsync()
    {
        await Shell.Current.GoToAsync("///expenses");
    }

    [RelayCommand]
    public async Task ViewAnalyticsAsync()
    {
        await Shell.Current.GoToAsync("///analytics");
    }

    [RelayCommand]
    public async Task ExpenseSelectedAsync(Expense? expense)
    {
        if (expense == null)
            return;

        var navParams = new Dictionary<string, object>
        {
            { "Expense", expense }
        };
        await Shell.Current.GoToAsync("expenseDetail", navParams);
    }
}
