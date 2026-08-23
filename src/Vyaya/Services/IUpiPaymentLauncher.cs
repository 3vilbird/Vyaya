using Vyaya.Models;

namespace Vyaya.Services;

public interface IUpiPaymentLauncher
{
    Task<UpiPaymentResult> LaunchAsync(UpiPaymentRequest request, decimal? overrideAmount = null);
    Task<UpiPaymentResult> LaunchPackageAsync(string packageName, UpiPaymentRequest request, decimal? overrideAmount = null);
    Task<bool> OpenAppDirectlyAsync(string packageName);
    bool IsSupported { get; }
}
