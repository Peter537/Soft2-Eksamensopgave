namespace MToGo.Website.Services.Payment;

public class PaymentContext
{
    private readonly Dictionary<string, IPaymentStrategy> _strategies;

    public PaymentContext(IEnumerable<IPaymentStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.PaymentMethodId, s => s);
    }

    public IEnumerable<IPaymentStrategy> GetAvailableStrategies()
    {
        return _strategies.Values.Where(s => s.IsAvailable());
    }

    public IPaymentStrategy? GetStrategy(string paymentMethodId)
    {
        return _strategies.TryGetValue(paymentMethodId, out var strategy) ? strategy : null;
    }

    public Task<PaymentResult> ProcessPaymentAsync(string paymentMethodId, PaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return Task.FromResult(PaymentResult.Failed("Amount must be greater than zero"));
        }

        var strategy = GetStrategy(paymentMethodId);
        if (strategy == null)
        {
            return Task.FromResult(PaymentResult.Failed("Unknown payment method"));
        }

        if (!strategy.IsAvailable())
        {
            return Task.FromResult(PaymentResult.Failed($"Payment method '{strategy.DisplayName}' is not available"));
        }

        return strategy.ProcessPaymentAsync(request);
    }
}
