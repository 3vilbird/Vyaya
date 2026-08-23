using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyaya.Models;
using Vyaya.Services;

namespace Vyaya.ViewModels;

public partial class ScanViewModel : BaseViewModel
{
    private readonly IUpiParser _upiParser;

    [ObservableProperty]
    private string _manualQrPayload = string.Empty;

    [ObservableProperty]
    private bool _isTorchOn;

    [ObservableProperty]
    private bool _isCameraActive = true;

    public ScanViewModel(IUpiParser upiParser)
    {
        _upiParser = upiParser;
        Title = "Scan & Pay";
    }

    [RelayCommand]
    public async Task ProcessPayloadAsync(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return;

        ClearError();

        var request = _upiParser.Parse(payload);
        if (request == null)
        {
            SetError("This QR code is not a supported UPI payment QR. Please scan a valid UPI QR code.");
            return;
        }

        // Navigate to confirmation page
        var navParams = new Dictionary<string, object>
        {
            { "PaymentRequest", request }
        };

        await Shell.Current.GoToAsync("expenseDetails", navParams);
    }

    [RelayCommand]
    public async Task SubmitManualPayloadAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualQrPayload))
        {
            SetError("Please enter or paste a valid UPI URI (e.g., upi://pay?pa=merchant@upi&pn=Merchant&am=450)");
            return;
        }

        await ProcessPayloadAsync(ManualQrPayload);
    }

    [RelayCommand]
    public void ToggleTorch()
    {
        IsTorchOn = !IsTorchOn;
    }

    [RelayCommand]
    public async Task PasteFromClipboardAsync()
    {
        try
        {
            if (Clipboard.Default.HasText)
            {
                var text = await Clipboard.Default.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ManualQrPayload = text;
                    await ProcessPayloadAsync(text);
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"Unable to read clipboard: {ex.Message}");
        }
    }
}
