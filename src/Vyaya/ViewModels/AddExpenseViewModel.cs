using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class AddExpenseViewModel : BaseViewModel
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string _titleOrMerchant = string.Empty;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private DateTime _expenseDate = DateTime.Today;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _userNote = string.Empty;

    [ObservableProperty]
    private string _categorySearchText = string.Empty;

    [ObservableProperty]
    private bool _isCategoryPickerOpen;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Category> FilteredCategories { get; } = new();
    public ObservableCollection<Category> QuickCategories { get; } = new();
    public List<PaymentMethod> AvailablePaymentMethods { get; } = Enum.GetValues<PaymentMethod>().ToList();

    public AddExpenseViewModel(IExpenseService expenseService, ICategoryService categoryService)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        Title = "Add Expense";
    }

    [RelayCommand]
    public async Task InitializeAsync()
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

        if (SelectedCategory == null)
        {
            SelectedCategory = all.FirstOrDefault(c => c.Name.Equals("Food", StringComparison.OrdinalIgnoreCase))
                               ?? all.FirstOrDefault();
        }
    }

    [RelayCommand]
    public void AppendDigit(string digit)
    {
        if (digit == "." && AmountText.Contains('.'))
            return;

        if (AmountText == "0" && digit != ".")
            AmountText = digit;
        else
            AmountText += digit;
    }

    [RelayCommand]
    public void Backspace()
    {
        if (!string.IsNullOrEmpty(AmountText))
        {
            AmountText = AmountText[..^1];
        }
    }

    [RelayCommand]
    public void ClearAmount()
    {
        AmountText = string.Empty;
    }

    [RelayCommand]
    public void AddQuickAmount(string valueStr)
    {
        if (decimal.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var addVal))
        {
            decimal current = 0;
            if (decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var cur))
            {
                current = cur;
            }
            AmountText = (current + addVal).ToString("0.##", CultureInfo.InvariantCulture);
        }
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
    public void SelectPaymentMethod(PaymentMethod method)
    {
        SelectedPaymentMethod = method;
    }

    [RelayCommand]
    public void ToggleCategoryPicker()
    {
        IsCategoryPickerOpen = !IsCategoryPickerOpen;
    }

    [RelayCommand]
    public async Task SaveExpenseAsync()
    {
        if (!decimal.TryParse(AmountText.Replace(",", "").Trim(), NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            SetError("Please enter a valid amount greater than zero.");
            return;
        }

        ClearError();
        IsBusy = true;

        try
        {
            var categoryName = SelectedCategory?.Name ?? "Other";
            var note = !string.IsNullOrWhiteSpace(UserNote) ? UserNote.Trim() : null;

            var expense = await _expenseService.CreateManualExpenseAsync(
                amount,
                categoryName,
                SelectedPaymentMethod,
                note,
                ExpenseDate
            );

            if (!string.IsNullOrWhiteSpace(TitleOrMerchant))
            {
                expense.MerchantName = TitleOrMerchant.Trim();
                await _expenseService.UpdateExpenseDetailsAsync(expense);
            }

            // Reset form
            AmountText = string.Empty;
            TitleOrMerchant = string.Empty;
            UserNote = string.Empty;

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            SetError($"Error saving expense: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
