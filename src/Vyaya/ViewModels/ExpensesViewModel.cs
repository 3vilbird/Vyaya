using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public class ExpenseDateGroup : ObservableCollection<Expense>
{
    public string DateHeader { get; set; } = string.Empty;
    public string DailyTotalFormatted { get; set; } = string.Empty;

    public ExpenseDateGroup(string header, string total, IEnumerable<Expense> expenses) : base(expenses)
    {
        DateHeader = header;
        DailyTotalFormatted = total;
    }
}

public partial class ExpensesViewModel : BaseViewModel
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedCategoryFilter = "All";

    [ObservableProperty]
    private string _selectedMethodFilter = "All";

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _hasNoExpenses;

    [ObservableProperty]
    private int _totalExpensesCount;

    public ObservableCollection<ExpenseDateGroup> GroupedExpenses { get; } = new();
    public ObservableCollection<string> CategoryFilters { get; } = new() { "All" };
    public List<string> MethodFilters { get; } = new() { "All", "Cash", "UPI", "CreditCard", "DebitCard", "BankTransfer", "Other" };
    public List<string> StatusFilters { get; } = new() { "All", "Paid", "Recorded", "Pending", "Failed", "Cancelled", "Unknown" };

    public ExpensesViewModel(IExpenseService expenseService, ICategoryService categoryService)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        Title = "Expense History";
    }

    [RelayCommand]
    public async Task LoadExpensesAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearError();

        try
        {
            // Populate category filter list if needed
            if (CategoryFilters.Count <= 1)
            {
                var categories = await _categoryService.GetCategoriesAsync();
                foreach (var cat in categories)
                {
                    CategoryFilters.Add(cat.Name);
                }
            }

            PaymentMethod? method = null;
            if (SelectedMethodFilter != "All" && Enum.TryParse<PaymentMethod>(SelectedMethodFilter, out var parsedMethod))
            {
                method = parsedMethod;
            }

            ExpenseStatus? status = null;
            if (SelectedStatusFilter != "All" && Enum.TryParse<ExpenseStatus>(SelectedStatusFilter, out var parsedStatus))
            {
                status = parsedStatus;
            }

            string? category = SelectedCategoryFilter != "All" ? SelectedCategoryFilter : null;

            var expenses = await _expenseService.SearchExpensesAsync(SearchQuery, category, method, status);
            TotalExpensesCount = expenses.Count;
            HasNoExpenses = TotalExpensesCount == 0;

            // Group by Date
            GroupedExpenses.Clear();
            var culture = new CultureInfo("en-IN");
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var groups = expenses
                .GroupBy(e => e.EffectiveDateLocal.Date)
                .OrderByDescending(g => g.Key);

            foreach (var g in groups)
            {
                var date = g.Key;
                string header;
                if (date == today)
                    header = $"Today • {date:d MMM}";
                else if (date == yesterday)
                    header = $"Yesterday • {date:d MMM}";
                else if (date.Year == today.Year)
                    header = date.ToString("dddd, d MMMM", CultureInfo.InvariantCulture);
                else
                    header = date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

                var dayTotal = g.Where(e => e.Status == ExpenseStatus.Paid || e.Status == ExpenseStatus.Recorded)
                                .Sum(e => e.Amount);
                var totalFormatted = dayTotal > 0 ? $"₹{dayTotal.ToString("N0", culture)}" : string.Empty;

                GroupedExpenses.Add(new ExpenseDateGroup(header, totalFormatted, g.OrderByDescending(e => e.EffectiveDateLocal)));
            }
        }
        catch (Exception ex)
        {
            SetError($"Error loading expenses: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync(string query)
    {
        SearchQuery = query;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    public async Task SetCategoryFilterAsync(string category)
    {
        SelectedCategoryFilter = category;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    public async Task SetMethodFilterAsync(string method)
    {
        SelectedMethodFilter = method;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    public async Task SetStatusFilterAsync(string status)
    {
        SelectedStatusFilter = status;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    public async Task SelectExpenseAsync(Expense? expense)
    {
        if (expense == null)
            return;

        var navParams = new Dictionary<string, object>
        {
            { "Expense", expense }
        };
        await Shell.Current.GoToAsync("expenseDetail", navParams);
    }

    [RelayCommand]
    public async Task DeleteExpenseAsync(Expense? expense)
    {
        if (expense == null || Shell.Current == null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Expense",
            $"Are you sure you want to delete {expense.DisplayTitle} ({expense.FormattedAmount})?",
            "Delete",
            "Cancel"
        );

        if (confirmed)
        {
            await _expenseService.DeleteExpenseAsync(expense.Id);
            await LoadExpensesAsync();
        }
    }
}
