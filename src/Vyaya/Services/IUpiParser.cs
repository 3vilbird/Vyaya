using Vyaya.Models;

namespace Vyaya.Services;

public interface IUpiParser
{
    UpiPaymentRequest? Parse(string payload);
    bool IsValidUpiPayload(string payload);
}
