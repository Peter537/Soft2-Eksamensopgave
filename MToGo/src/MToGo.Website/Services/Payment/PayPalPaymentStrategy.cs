namespace MToGo.Website.Services.Payment;

public class PayPalPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "paypal";
    public string DisplayName => "PayPal";
    public string IconClass => "bi-paypal";
    public string Description => "Pay with your PayPal account";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request.Details is not PayPalDetails details)
        {
            return Task.FromResult(PaymentResult.Failed("PayPal details are required"));
        }

        if (string.IsNullOrWhiteSpace(details.Email) || !details.Email.Contains('@') || !details.Email.Contains('.'))
        {
            return Task.FromResult(PaymentResult.Failed("Invalid PayPal email"));
        }

        var transactionId = $"PP-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.OrderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
