namespace MToGo.Website.Services.Payment;

public class GooglePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "google_pay";
    public string DisplayName => "Google Pay";
    public string IconClass => "bi-google";
    public string Description => "Pay with Google Pay";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request.Details is not TokenDetails details)
        {
            return Task.FromResult(PaymentResult.Failed("Google Pay token is required"));
        }

        if (string.IsNullOrWhiteSpace(details.Token) || details.Token.Length < 8)
        {
            return Task.FromResult(PaymentResult.Failed("Invalid Google Pay token"));
        }

        var transactionId = $"GP-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.OrderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
