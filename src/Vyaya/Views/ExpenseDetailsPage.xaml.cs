using Vyaya.ViewModels;

namespace Vyaya.Views;

public partial class ExpenseDetailsPage : ContentPage
{
    public ExpenseDetailsPage(ExpenseDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
