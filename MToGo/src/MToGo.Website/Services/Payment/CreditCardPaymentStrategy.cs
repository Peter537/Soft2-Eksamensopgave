namespace MToGo.Website.Services.Payment;

public class CreditCardPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "credit_card";
    public string DisplayName => "Credit Card";
    public string IconClass => "bi-credit-card";
    public string Description => "Pay securely with Visa, Mastercard, or American Express";

    public Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        var transactionId = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
