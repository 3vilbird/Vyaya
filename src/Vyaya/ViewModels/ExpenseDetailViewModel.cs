using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class ExpenseDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IExpenseService _expenseService;
    private readonly IUpiPaymentLauncher _paymentLauncher;
    private readonly IUpiParser _upiParser;

    [ObservableProperty]
    private Expense? _expense;

    [ObservableProperty]
    private bool _canReconcile;

    [ObservableProperty]
    private bool _isUpiScan;

    public ExpenseDetailViewModel(
        IExpenseService expenseService,
        IUpiPaymentLauncher paymentLauncher,
        IUpiParser upiParser)
    {
        _expenseService = expenseService;
        _paymentLauncher = paymentLauncher;
        _upiParser = upiParser;
        Title = "Expense Receipt";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Expense", out var expObj) && expObj is Expense exp)
        {
            SetCurrentExpense(exp);
        }
    }

    private void SetCurrentExpense(Expense exp)
    {
        Expense = exp;
        IsUpiScan = exp.EntryType == ExpenseEntryType.UpiScan;
        CanReconcile = exp.Status == ExpenseStatus.Pending || exp.Status == ExpenseStatus.Unknown;
    }

    [RelayCommand]
    public async Task MarkAsPaidAsync()
    {
        if (Expense == null || Shell.Current == null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Confirm Payment", "Mark this transaction as Paid?", "Yes, Mark Paid", "Cancel");
        if (confirmed)
        {
            await _expenseService.UpdateExpenseStatusAsync(Expense.Id, ExpenseStatus.Paid);
            Expense.Status = ExpenseStatus.Paid;
            Expense.CompletedAtUtc = DateTime.UtcNow;
            SetCurrentExpense(Expense);
        }
    }

    [RelayCommand]
    public async Task MarkAsCancelledAsync()
    {
        if (Expense == null || Shell.Current == null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Cancel Payment", "Mark this transaction as Cancelled?", "Yes, Cancelled", "Cancel");
        if (confirmed)
        {
            await _expenseService.UpdateExpenseStatusAsync(Expense.Id, ExpenseStatus.Cancelled);
            Expense.Status = ExpenseStatus.Cancelled;
            SetCurrentExpense(Expense);
        }
    }

    [RelayCommand]
    public async Task MarkAsFailedAsync()
    {
        if (Expense == null)
            return;

        await _expenseService.UpdateExpenseStatusAsync(Expense.Id, ExpenseStatus.Failed);
        Expense.Status = ExpenseStatus.Failed;
        SetCurrentExpense(Expense);
    }

    [RelayCommand]
    public async Task RelaunchUpiPaymentAsync()
    {
        if (Expense == null || string.IsNullOrWhiteSpace(Expense.RawUpiPayload))
        {
            SetError("No UPI data available to relaunch payment.");
            return;
        }

        var request = _upiParser.Parse(Expense.RawUpiPayload);
        if (request != null)
        {
            await _paymentLauncher.LaunchAsync(request, Expense.Amount);
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (Expense == null || Shell.Current == null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Delete Expense", "Are you sure you want to delete this expense record permanently?", "Delete", "Cancel");
        if (confirmed)
        {
            await _expenseService.DeleteExpenseAsync(Expense.Id);
            await Shell.Current.GoToAsync("..");
        }
    }
}
