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
            var context = activity ?? Android.App.Application.Context;

            var upiUri = Android.Net.Uri.Parse(uriString);
            var intent = new Intent(Intent.ActionView, upiUri);
            intent.AddFlags(ActivityFlags.NewTask);

            // Strategy 1: Open system chooser directly
            try
            {
                var chooser = Intent.CreateChooser(intent, "Pay with UPI");
                if (activity != null)
                {
                    activity.StartActivity(chooser);
                }
                else
                {
                    chooser.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(chooser);
                }
                return UpiPaymentResult.Unknown(uriString, "UPI payment application launched.");
            }
            catch (ActivityNotFoundException)
            {
                // Chooser failed, try direct intent
            }

            // Strategy 2: Direct intent without chooser
            try
            {
                if (activity != null)
                {
                    activity.StartActivity(intent);
                }
                else
                {
                    context.StartActivity(intent);
                }
                return UpiPaymentResult.Unknown(uriString, "UPI payment application launched.");
            }
            catch (ActivityNotFoundException)
            {
                // Direct failed, try targeting specific known UPI apps directly
            }

            // Strategy 3: Check and launch known installed UPI packages directly (PhonePe, GPay, Paytm, etc.)
            var knownPackages = new[]
            {
                "com.phonepe.app",
                "com.google.android.apps.nbu.paisa.user",
                "net.one97.paytm",
                "in.org.npci.upiapp",
                "com.dreamplug.androidapp",
                "in.amazon.mShop.android.shopping",
                "org.slice.app",
                "club.slice.android",
                "com.upi.axispay",
                "com.mobikwik_new"
            };

            foreach (var pkg in knownPackages)
            {
                try
                {
                    var appIntent = new Intent(Intent.ActionView, upiUri);
                    appIntent.SetPackage(pkg);
                    if (activity != null)
                    {
                        appIntent.AddFlags(ActivityFlags.NewTask);
                        activity.StartActivity(appIntent);
                    }
                    else
                    {
                        appIntent.AddFlags(ActivityFlags.NewTask);
                        context.StartActivity(appIntent);
                    }
                    return UpiPaymentResult.Unknown(uriString, $"Launched UPI application ({pkg}).");
                }
                catch
                {
                    // Continue to next package
                }
            }

            // Strategy 4: Fallback to MAUI Launcher
            try
            {
                await Launcher.Default.OpenAsync(new System.Uri(uriString));
                return UpiPaymentResult.Unknown(uriString, "Launched via system launcher.");
            }
            catch
            {
                return UpiPaymentResult.Failed("No compatible UPI application (PhonePe, Google Pay, Paytm, etc.) was found on this device.");
            }
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
