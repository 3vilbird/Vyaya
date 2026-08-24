using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class ExpenseDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IUpiPaymentLauncher _paymentLauncher;

    [ObservableProperty]
    private UpiPaymentRequest? _paymentRequest;

    [ObservableProperty]
    private string _payeeName = string.Empty;

    [ObservableProperty]
    private string _paymentAddress = string.Empty;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string _currency = "INR";

    [ObservableProperty]
    private string _upiTransactionNote = string.Empty;

    [ObservableProperty]
    private string _userNote = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _categorySearchText = string.Empty;

    [ObservableProperty]
    private bool _isCategoryPickerOpen;

    [ObservableProperty]
    private bool _isCopiedToastVisible;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Category> FilteredCategories { get; } = new();
    public ObservableCollection<Category> QuickCategories { get; } = new();

    public ExpenseDetailsViewModel(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IUpiPaymentLauncher paymentLauncher)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _paymentLauncher = paymentLauncher;
        Title = "Confirm Payment";
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("PaymentRequest", out var reqObj) && reqObj is UpiPaymentRequest request)
        {
            PaymentRequest = request;
            PayeeName = !string.IsNullOrWhiteSpace(request.PayeeName) ? request.PayeeName : "Merchant";
            PaymentAddress = request.PaymentAddress ?? string.Empty;
            Currency = !string.IsNullOrWhiteSpace(request.Currency) ? request.Currency : "INR";
            UpiTransactionNote = request.TransactionNote ?? string.Empty;

            if (request.Amount.HasValue && request.Amount.Value > 0)
            {
                AmountText = request.Amount.Value.ToString("0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                AmountText = string.Empty;
            }

            await LoadCategoriesAsync();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var all = await _categoryService.GetCategoriesAsync();
        Categories.Clear();
        FilteredCategories.Clear();
        QuickCategories.Clear();

        foreach (var cat in all)
        {
            Categories.Add(cat);
            FilteredCategories.Add(cat);
        }

        foreach (var cat in all.Take(6))
        {
            QuickCategories.Add(cat);
        }

        // Set default category (Other or Groceries/Food based on context)
        SelectedCategory = all.FirstOrDefault(c => c.Name.Equals("Other", StringComparison.OrdinalIgnoreCase)) 
                           ?? all.FirstOrDefault();
    }

    [RelayCommand]
    public void FilterCategories(string query)
    {
        CategorySearchText = query;
        FilteredCategories.Clear();

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var cat in Categories)
            {
                FilteredCategories.Add(cat);
            }
            return;
        }

        var trimmed = query.Trim().ToLowerInvariant();
        var matches = Categories
            .Where(c => c.Name.ToLowerInvariant().Contains(trimmed) || 
                       (!string.IsNullOrEmpty(c.Keywords) && c.Keywords.ToLowerInvariant().Contains(trimmed)))
            .OrderByDescending(c => c.Name.ToLowerInvariant().StartsWith(trimmed));

        foreach (var cat in matches)
        {
            FilteredCategories.Add(cat);
        }
    }

    [RelayCommand]
    public void SelectCategory(Category? category)
    {
        if (category != null)
        {
            SelectedCategory = category;
            IsCategoryPickerOpen = false;
        }
    }

    [RelayCommand]
    public void ToggleCategoryPicker()
    {
        IsCategoryPickerOpen = !IsCategoryPickerOpen;
    }

    [RelayCommand]
    public async Task CopyUpiIdAsync()
    {
        if (!string.IsNullOrWhiteSpace(PaymentAddress))
        {
            try
            {
                await Clipboard.Default.SetTextAsync(PaymentAddress);
                IsCopiedToastVisible = true;
                await Task.Delay(2000);
                IsCopiedToastVisible = false;
            }
            catch { }
        }
    }

    [RelayCommand]
    public async Task PayWithUpiAsync()
    {
        await ExecutePaymentFlowAsync(null);
    }

    [RelayCommand]
    public async Task PayWithPhonePeAsync()
    {
        await ExecutePaymentFlowAsync("com.phonepe.app");
    }

    [RelayCommand]
    public async Task PayWithGPayAsync()
    {
        await ExecutePaymentFlowAsync("com.google.android.apps.nbu.paisa.user");
    }

    [RelayCommand]
    public async Task PayWithPaytmAsync()
    {
        await ExecutePaymentFlowAsync("net.one97.paytm");
    }

    private async Task ExecutePaymentFlowAsync(string? specificPackage)
    {
        if (PaymentRequest == null)
        {
            SetError("Invalid payment request.");
            return;
        }

        if (!decimal.TryParse(AmountText.Replace(",", "").Trim(), NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            SetError("Please enter a valid amount greater than zero.");
            return;
        }

        ClearError();
        IsBusy = true;

        try
        {
            // Auto copy UPI address for seamless clipboard pasting
            if (!string.IsNullOrWhiteSpace(PaymentAddress))
            {
                try { await Clipboard.Default.SetTextAsync(PaymentAddress); } catch { }
            }

            var categoryName = SelectedCategory?.Name ?? "Other";

            // CRITICAL STEP: Save as Pending in SQLite BEFORE launching UPI application
            var pendingExpense = await _expenseService.CreatePendingUpiExpenseAsync(
                PaymentRequest,
                amount,
                categoryName,
                UserNote
            );

            // Launch UPI application
            UpiPaymentResult result;
            if (!string.IsNullOrEmpty(specificPackage))
            {
                result = await _paymentLauncher.LaunchPackageAsync(specificPackage, PaymentRequest, amount);
            }
            else
            {
                result = await _paymentLauncher.LaunchAsync(PaymentRequest, amount);
            }

            if (result.Status == ExpenseStatus.Failed && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                await _expenseService.UpdateExpenseStatusAsync(pendingExpense.Id, ExpenseStatus.Failed);
                SetError(result.ErrorMessage);
                return;
            }

            // Return to Home
            await Shell.Current.GoToAsync("///home");
        }
        catch (Exception ex)
        {
            SetError($"Error initiating payment: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
