namespace MToGo.Website.Services.Payment;

public class MobilePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "mobilepay";
    public string DisplayName => "MobilePay";
    public string IconClass => "bi-phone";
    public string Description => "Pay with MobilePay - fast and easy";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request.Details is not TokenDetails details)
        {
            return Task.FromResult(PaymentResult.Failed("MobilePay token is required"));
        }

        if (string.IsNullOrWhiteSpace(details.Token) || details.Token.Length < 8)
        {
            return Task.FromResult(PaymentResult.Failed("Invalid MobilePay token"));
        }

        var transactionId = $"MP-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.OrderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
