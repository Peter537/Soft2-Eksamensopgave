namespace MToGo.Website.Services.Payment;

public class MobilePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "mobilepay";
    public string DisplayName => "MobilePay";
    public string IconClass => "bi-phone";
    public string Description => "Pay with MobilePay - fast and easy";

    public Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        var transactionId = $"MP-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
