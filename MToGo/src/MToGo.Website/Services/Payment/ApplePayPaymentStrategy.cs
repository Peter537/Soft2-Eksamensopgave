namespace MToGo.Website.Services.Payment;

public class ApplePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "apple_pay";
    public string DisplayName => "Apple Pay";
    public string IconClass => "bi-apple";
    public string Description => "Pay with Apple Pay";

    public Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        var transactionId = $"AP-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
