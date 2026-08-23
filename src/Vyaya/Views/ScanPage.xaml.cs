using ZXing.Net.Maui;
using Vyaya.ViewModels;

namespace Vyaya.Views;

public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _viewModel;
    private bool _isProcessingBarcode;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Configure Barcode Reader with TryHarder and TryInverted for PhonePe/UPI QR codes
        BarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            TryHarder = true,
            TryInverted = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isProcessingBarcode = false;

        await CheckAndRequestCameraPermissionAsync();

        // Brief delay before activating detection to let Camera2 initialize preview
        await Task.Delay(200);
        BarcodeReaderView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReaderView.IsDetecting = false;
    }

    private async Task CheckAndRequestCameraPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                BarcodeReaderView.IsDetecting = true;
            }
            else
            {
                BarcodeReaderView.IsDetecting = false;
                await DisplayAlertAsync(
                    "Camera Permission Required",
                    "Camera access is needed to scan UPI QR codes. You can also paste or enter a UPI link manually below.",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Camera Error", $"Unable to start camera: {ex.Message}", "OK");
        }
    }

    private void BarcodeReaderView_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessingBarcode)
            return;

        var detectedBarcode = e.Results?.FirstOrDefault();
        if (detectedBarcode == null || string.IsNullOrWhiteSpace(detectedBarcode.Value))
            return;

        _isProcessingBarcode = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Pause detection so we don't scan repeatedly
                BarcodeReaderView.IsDetecting = false;

                // Provide haptic feedback
                try
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch
                {
                    // Ignore haptic errors on platforms without vibration
                }

                // Process scanned QR payload (navigates to confirmation page)
                await _viewModel.ProcessPayloadAsync(detectedBarcode.Value);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Scan Error", $"Error reading QR code: {ex.Message}", "OK");
            }
            finally
            {
                // Reset processing flag
                _isProcessingBarcode = false;
            }
        });
    }
}
