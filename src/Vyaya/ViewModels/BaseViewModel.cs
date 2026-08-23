using CommunityToolkit.Mvvm.ComponentModel;

namespace Vyaya.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }

    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
