using Vyaya.Models;

#if ANDROID
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Microsoft.Maui.ApplicationModel;
#endif

namespace Vyaya.Services;

public class UpiPaymentLauncher : IUpiPaymentLauncher
{
    public bool IsSupported
    {
        get
        {
#if ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    public async Task<UpiPaymentResult> LaunchAsync(UpiPaymentRequest request, decimal? overrideAmount = null)
    {
        var uriString = request.BuildUpiUri(overrideAmount);

#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                return UpiPaymentResult.Failed("No active Android activity found.");
            }

            var upiUri = Android.Net.Uri.Parse(uriString);
            var intent = new Intent(Intent.ActionView, upiUri);

            // Check if there are apps available to handle the UPI intent
            var packageManager = activity.PackageManager;
            if (packageManager == null)
            {
                return UpiPaymentResult.Failed("Unable to access Android PackageManager.");
            }

            var resolveInfos = packageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            if (resolveInfos == null || resolveInfos.Count == 0)
            {
                return UpiPaymentResult.Failed("No compatible UPI applications (PhonePe, Google Pay, Paytm, etc.) found on this device.");
            }

            var chooser = Intent.CreateChooser(intent, "Pay using UPI");
            activity.StartActivity(chooser);

            // Launched successfully - the payment status will be Pending/Unknown until user returns or reconciles
            return UpiPaymentResult.Unknown(uriString, "UPI payment application launched.");
        }
        catch (ActivityNotFoundException)
        {
            return UpiPaymentResult.Failed("No compatible UPI application was found on this device.");
        }
        catch (Exception ex)
        {
            return UpiPaymentResult.Failed($"Error launching UPI payment: {ex.Message}");
        }
#else
        try
        {
            var canOpen = await Launcher.Default.CanOpenAsync(new Uri(uriString));
            if (canOpen)
            {
                await Launcher.Default.OpenAsync(new Uri(uriString));
                return UpiPaymentResult.Unknown(uriString, "UPI application opened via system launcher.");
            }
            return UpiPaymentResult.Failed("UPI launcher is not supported on this platform.");
        }
        catch (Exception ex)
        {
            return UpiPaymentResult.Failed($"Error: {ex.Message}");
        }
#endif
    }
}
