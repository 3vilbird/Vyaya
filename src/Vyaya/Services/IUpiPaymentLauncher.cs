using Vyaya.Models;

namespace Vyaya.Services;

public interface IUpiPaymentLauncher
{
    Task<UpiPaymentResult> LaunchAsync(UpiPaymentRequest request, decimal? overrideAmount = null);
    bool IsSupported { get; }
}
