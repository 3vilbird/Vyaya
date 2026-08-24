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

            // Copy UPI address to clipboard as background safety
            if (!string.IsNullOrWhiteSpace(request.PaymentAddress))
            {
                try { await Clipboard.Default.SetTextAsync(request.PaymentAddress); } catch { }
            }

            // Strategy 1: Open system chooser
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
                    intent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(intent);
                }
                return UpiPaymentResult.Unknown(uriString, "UPI payment application launched.");
            }
            catch (ActivityNotFoundException)
            {
                // Direct failed, try targeting specific known UPI apps
            }

            // Strategy 3: Target known UPI packages directly (PhonePe, GPay, Paytm, etc.)
            var knownPackages = new[]
            {
                "com.phonepe.app",
                "com.google.android.apps.nbu.paisa.user",
                "net.one97.paytm",
                "in.org.npci.upiapp",
                "com.dreamplug.androidapp",
                "in.amazon.mShop.android.shopping",
                "org.slice.app",
                "club.slice.android"
            };

            foreach (var pkg in knownPackages)
            {
                try
                {
                    var appIntent = new Intent(Intent.ActionView, upiUri);
                    appIntent.SetPackage(pkg);

                    if (activity != null)
                    {
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
            var canOpen = await Launcher.Default.CanOpenAsync(new System.Uri(uriString));
            if (canOpen)
            {
                await Launcher.Default.OpenAsync(new System.Uri(uriString));
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

    public async Task<UpiPaymentResult> LaunchPackageAsync(string packageName, UpiPaymentRequest request, decimal? overrideAmount = null)
    {
        var uriString = request.BuildUpiUri(overrideAmount);

#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            var context = activity ?? Android.App.Application.Context;

            // Copy UPI address to clipboard as background safety
            if (!string.IsNullOrWhiteSpace(request.PaymentAddress))
            {
                try { await Clipboard.Default.SetTextAsync(request.PaymentAddress); } catch { }
            }

            var upiUri = Android.Net.Uri.Parse(uriString);
            var appIntent = new Intent(Intent.ActionView, upiUri);
            appIntent.SetPackage(packageName);

            try
            {
                if (activity != null)
                {
                    activity.StartActivity(appIntent);
                }
                else
                {
                    appIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(appIntent);
                }
                return UpiPaymentResult.Unknown(uriString, $"Launched {packageName}.");
            }
            catch (ActivityNotFoundException)
            {
                // If specific intent failed, open the app directly
                var opened = await OpenAppDirectlyAsync(packageName);
                if (opened)
                {
                    return UpiPaymentResult.Unknown(uriString, $"Opened {packageName} directly.");
                }
                return UpiPaymentResult.Failed($"The requested application ({packageName}) is not installed.");
            }
        }
        catch (Exception ex)
        {
            return UpiPaymentResult.Failed($"Error: {ex.Message}");
        }
#else
        return await LaunchAsync(request, overrideAmount);
#endif
    }

    public Task<bool> OpenAppDirectlyAsync(string packageName)
    {
#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            var context = activity ?? Android.App.Application.Context;
            var pm = context.PackageManager;

            if (pm != null)
            {
                var launchIntent = pm.GetLaunchIntentForPackage(packageName);
                if (launchIntent != null)
                {
                    launchIntent.AddFlags(ActivityFlags.NewTask);
                    if (activity != null)
                    {
                        activity.StartActivity(launchIntent);
                    }
                    else
                    {
                        context.StartActivity(launchIntent);
                    }
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
#else
        return Task.FromResult(false);
#endif
    }
}
