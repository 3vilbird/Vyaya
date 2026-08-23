using Vyaya.Services;
using Xunit;

namespace Vyaya.Tests.Services;

public class UpiParserTests
{
    private readonly UpiParser _parser = new();

    [Fact]
    public void Parse_ValidStandardUpiUri_ReturnsParsedRequest()
    {
        var uri = "upi://pay?pa=test@upi&pn=Test&am=100&cu=INR";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("test@upi", result.PaymentAddress);
        Assert.Equal("Test", result.PayeeName);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("INR", result.Currency);
        Assert.True(_parser.IsValidUpiPayload(uri));
    }

    [Fact]
    public void Parse_PhonePeScheme_ReturnsParsedRequest()
    {
        var uri = "phonepe://pay?pa=9876543210@ybl&pn=PhonePe%20Merchant&am=450.00&cu=INR";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("9876543210@ybl", result.PaymentAddress);
        Assert.Equal("PhonePe Merchant", result.PayeeName);
        Assert.Equal(450m, result.Amount);
    }

    [Fact]
    public void Parse_IntentUrlWithEmbeddedUpi_ReturnsParsedRequest()
    {
        var uri = "intent://pay?pa=merchant@okaxis&pn=Grocery&am=200#Intent;scheme=upi;package=com.phonepe.app;end";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("merchant@okaxis", result.PaymentAddress);
        Assert.Equal("Grocery", result.PayeeName);
        Assert.Equal(200m, result.Amount);
    }

    [Fact]
    public void Parse_BareVpaAddress_ReturnsParsedRequest()
    {
        var vpa = "merchant.store@ybl";
        var result = _parser.Parse(vpa);

        Assert.NotNull(result);
        Assert.Equal("merchant.store@ybl", result.PaymentAddress);
        Assert.Equal("INR", result.Currency);
    }

    [Fact]
    public void Parse_MissingAmount_ReturnsNullAmount()
    {
        var uri = "upi://pay?pa=merchant@okaxis&pn=Merchant";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("merchant@okaxis", result.PaymentAddress);
        Assert.Equal("Merchant", result.PayeeName);
        Assert.Null(result.Amount);
        Assert.Equal("INR", result.Currency);
    }

    [Fact]
    public void Parse_MissingPayeeName_ReturnsNullPayeeName()
    {
        var uri = "upi://pay?pa=user@paytm&am=250.50";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("user@paytm", result.PaymentAddress);
        Assert.Null(result.PayeeName);
        Assert.Equal(250.50m, result.Amount);
    }

    [Fact]
    public void Parse_UrlEncodedParameters_CorrectlyDecodes()
    {
        var uri = "upi://pay?pa=abc@upi&pn=ABC%20Vegetables&tn=Weekly%20Supplies&tr=TXN%2312345";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("abc@upi", result.PaymentAddress);
        Assert.Equal("ABC Vegetables", result.PayeeName);
        Assert.Equal("Weekly Supplies", result.TransactionNote);
        Assert.Equal("TXN#12345", result.TransactionReference);
    }

    [Fact]
    public void Parse_PlusSignEncodedParameters_CorrectlyDecodes()
    {
        var uri = "upi://pay?pa=merchant@upi&pn=Green+Grocery+Store&tn=Fruits+and+veggies";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("Green Grocery Store", result.PayeeName);
        Assert.Equal("Fruits and veggies", result.TransactionNote);
    }

    [Fact]
    public void Parse_CaseInsensitiveSchemeAndParams_ParsesSuccessfully()
    {
        var uri = "UPI://PAY?PA=merchant@upi&PN=Store&AM=450.00&CU=INR";
        var result = _parser.Parse(uri);

        Assert.NotNull(result);
        Assert.Equal("merchant@upi", result.PaymentAddress);
        Assert.Equal("Store", result.PayeeName);
        Assert.Equal(450m, result.Amount);
    }

    [Theory]
    [InlineData("https://example.com/pay")]
    [InlineData("random text without at symbol")]
    [InlineData("upi://invalid?something=1")]
    [InlineData("upi://pay?")]
    [InlineData("upi://pay?pn=NoAddress")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_InvalidPayloads_ReturnsNull(string? invalidPayload)
    {
        var result = _parser.Parse(invalidPayload!);
        Assert.Null(result);
        Assert.False(_parser.IsValidUpiPayload(invalidPayload!));
    }

    [Fact]
    public void BuildUpiUri_WithOverrideAmount_BuildsValidUri()
    {
        var request = new Models.UpiPaymentRequest
        {
            PaymentAddress = "test@upi",
            PayeeName = "Test Merchant",
            Currency = "INR"
        };

        var uri = request.BuildUpiUri(500m);
        Assert.Contains("pa=test%40upi", uri);
        Assert.Contains("pn=Test%20Merchant", uri);
        Assert.Contains("am=500.00", uri);
        Assert.Contains("cu=INR", uri);
    }
}
