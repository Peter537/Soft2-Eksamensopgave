namespace MToGo.Website.Services.Payment;

public class PaymentContext
{
    private readonly Dictionary<string, IPaymentStrategy> _strategies;
    private IPaymentStrategy? _currentStrategy;

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

    public bool SetStrategy(string paymentMethodId)
    {
        if (_strategies.TryGetValue(paymentMethodId, out var strategy))
        {
            _currentStrategy = strategy;
            return true;
        }
        return false;
    }

    public IPaymentStrategy? CurrentStrategy => _currentStrategy;

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, int orderId)
    {
        if (_currentStrategy == null)
        {
            return PaymentResult.Failed("No payment method selected");
        }

        if (!_currentStrategy.IsAvailable())
        {
            return PaymentResult.Failed($"Payment method '{_currentStrategy.DisplayName}' is not available");
        }

        return await _currentStrategy.ProcessPaymentAsync(amount, orderId);
    }
}
