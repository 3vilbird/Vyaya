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

        // High-Performance QR-Only Configuration (Eliminates 1D barcode overhead for instant PhonePe/UPI detection)
        BarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            TryHarder = false,
            TryInverted = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isProcessingBarcode = false;

        await CheckAndRequestCameraPermissionAsync();

        // Brief delay before activating detection to ensure camera preview is active
        await Task.Delay(150);
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
                // Pause detection immediately to avoid double scans
                BarcodeReaderView.IsDetecting = false;

                // Provide instant haptic feedback
                try
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch
                {
                    // Ignore haptic errors on unsupported hardware
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
                _isProcessingBarcode = false;
            }
        });
    }
}
