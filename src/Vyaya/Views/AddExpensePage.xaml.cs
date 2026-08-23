using Vyaya.ViewModels;

namespace Vyaya.Views;

public partial class AddExpensePage : ContentPage
{
    private readonly AddExpenseViewModel _viewModel;

    public AddExpensePage(AddExpenseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
