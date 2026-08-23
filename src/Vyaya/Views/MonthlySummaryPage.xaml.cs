using Vyaya.ViewModels;

namespace Vyaya.Views;

public partial class MonthlySummaryPage : ContentPage
{
    private readonly MonthlySummaryViewModel _viewModel;

    public MonthlySummaryPage(MonthlySummaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSummaryAsync();
    }
}
