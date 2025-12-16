namespace MToGo.Website.Services.Payment;

public class PayPalPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "paypal";
    public string DisplayName => "PayPal";
    public string IconClass => "bi-paypal";
    public string Description => "Pay with your PayPal account";

    public Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        var transactionId = $"PP-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
