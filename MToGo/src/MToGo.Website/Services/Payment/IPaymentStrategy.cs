namespace MToGo.Website.Services.Payment;

public interface IPaymentStrategy
{
    string PaymentMethodId { get; }
    string DisplayName { get; }
    string IconClass { get; }
    string Description { get; }
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId);
    bool IsAvailable();
}
 
