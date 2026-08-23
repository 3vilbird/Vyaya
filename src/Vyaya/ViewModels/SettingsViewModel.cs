using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Data;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IExpenseService _expenseService;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private int _totalExpensesCount;

    [ObservableProperty]
    private string _totalLifetimeSpend = "₹0";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    public SettingsViewModel(
        IExpenseService expenseService,
        IExpenseRepository expenseRepository,
        IExportService exportService)
    {
        _expenseService = expenseService;
        _expenseRepository = expenseRepository;
        _exportService = exportService;
        Title = "Settings & Export";
    }

    [RelayCommand]
    public async Task LoadInfoAsync()
    {
        ClearError();
        try
        {
            var count = await _expenseRepository.GetCountAsync();
            TotalExpensesCount = count;

            var all = await _expenseService.GetAllExpensesAsync();
            var validExpenses = all.Where(e => e.Status == ExpenseStatus.Paid || e.Status == ExpenseStatus.Recorded);
            var sum = validExpenses.Sum(e => e.Amount);

            var culture = new CultureInfo("en-IN");
            TotalLifetimeSpend = sum % 1 == 0
                ? $"₹{sum.ToString("N0", culture)}"
                : $"₹{sum.ToString("N2", culture)}";

            DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "vyaya_expenses.db3");
        }
        catch (Exception ex)
        {
            SetError($"Error reading settings: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExportAllExpensesAsync()
    {
        IsBusy = true;
        try
        {
            var expenses = await _expenseService.GetAllExpensesAsync();
            if (expenses.Count == 0)
            {
                SetError("No expenses recorded yet to export.");
                return;
            }

            var filePath = await _exportService.ExportExpensesToFileAsync(expenses, "Vyaya_All_Expenses");
            await _exportService.ShareExportedFileAsync(filePath, "Export All Vyaya Expenses");
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

    [RelayCommand]
    public async Task ClearAllDataAsync()
    {
        if (Shell.Current == null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "⚠️ Reset All Data",
            "Are you sure you want to delete ALL expense records? This action is permanent and cannot be undone.",
            "Delete All",
            "Cancel"
        );

        if (confirmed)
        {
            await _expenseRepository.ClearAllAsync();
            await LoadInfoAsync();
            await Shell.Current.DisplayAlertAsync("Database Cleared", "All local expenses have been removed.", "OK");
        }
    }
}
