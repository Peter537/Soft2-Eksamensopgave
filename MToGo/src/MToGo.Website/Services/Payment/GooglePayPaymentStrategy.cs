namespace MToGo.Website.Services.Payment;

public class GooglePayPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "google_pay";
    public string DisplayName => "Google Pay";
    public string IconClass => "bi-google";
    public string Description => "Pay with Google Pay";

    public Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        var transactionId = $"GP-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;
}
