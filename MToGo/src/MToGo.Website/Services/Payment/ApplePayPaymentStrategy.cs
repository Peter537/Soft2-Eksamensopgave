namespace MToGo.Website.Services.Payment;

public class ApplePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "apple_pay";
    public string DisplayName => "Apple Pay";
    public string IconClass => "bi-apple";
    public string Description => "Pay with Apple Pay";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request.Details is not TokenDetails details)
        {
            return Task.FromResult(PaymentResult.Failed("Apple Pay token is required"));
        }

        if (string.IsNullOrWhiteSpace(details.Token) || details.Token.Length < 8)
        {
            return Task.FromResult(PaymentResult.Failed("Invalid Apple Pay token"));
        }

        var transactionId = $"AP-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.OrderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
