using MToGo.Website.Services.Payment;

namespace MToGo.Website.Tests.Payment;

public class PaymentContextTests
{
    private static PaymentContext CreatePaymentContext()
    {
        var strategies = new IPaymentStrategy[]
        {
            new CreditCardPaymentStrategy(),
            new PayPalPaymentStrategy(),
            new MobilePayPaymentStrategy(),
            new ApplePayPaymentStrategy(),
            new GooglePayPaymentStrategy()
        };
        return new PaymentContext(strategies);
    }

    private static PaymentRequest CreatePaymentRequest(decimal amount, int orderId, string paymentMethodId)
    {
        var details = paymentMethodId switch
        {
            "credit_card" => (PaymentDetails)new CreditCardDetails("4111111111111111", "12", "2027", "123"),
            "paypal" => new PayPalDetails("test@example.com"),
            "mobilepay" or "apple_pay" or "google_pay" => new TokenDetails("test-token-123"),
            _ => new TokenDetails("default-token")
        };
        return new PaymentRequest(amount, orderId, details);
    }

    #region GetAvailableStrategies Tests

    [Fact]
    public void GetAvailableStrategies_ReturnsAllStrategies()
    {
        var context = CreatePaymentContext();
        var strategies = context.GetAvailableStrategies().ToList();

        Assert.Equal(5, strategies.Count);
    }

    [Fact]
    public void GetAvailableStrategies_ContainsAllPaymentMethods()
    {
        var context = CreatePaymentContext();
        var strategies = context.GetAvailableStrategies().ToList();

        var paymentMethodIds = strategies.Select(s => s.PaymentMethodId).ToList();

        Assert.Contains("credit_card", paymentMethodIds);
        Assert.Contains("paypal", paymentMethodIds);
        Assert.Contains("mobilepay", paymentMethodIds);
        Assert.Contains("apple_pay", paymentMethodIds);
        Assert.Contains("google_pay", paymentMethodIds);
    }

    #endregion

    #region GetStrategy Tests

    [Fact]
    public void GetStrategy_ValidId_ReturnsStrategy()
    {
        var context = CreatePaymentContext();
        var strategy = context.GetStrategy("credit_card");

        Assert.NotNull(strategy);
        Assert.Equal("credit_card", strategy.PaymentMethodId);
    }

    [Fact]
    public void GetStrategy_InvalidId_ReturnsNull()
    {
        var context = CreatePaymentContext();
        var strategy = context.GetStrategy("invalid_method");

        Assert.Null(strategy);
    }

    [Theory]
    [InlineData("credit_card")]
    [InlineData("paypal")]
    [InlineData("mobilepay")]
    [InlineData("apple_pay")]
    [InlineData("google_pay")]
    public void GetStrategy_AllValidIds_ReturnCorrectStrategy(string paymentMethodId)
    {
        var context = CreatePaymentContext();
        var strategy = context.GetStrategy(paymentMethodId);

        Assert.NotNull(strategy);
        Assert.Equal(paymentMethodId, strategy.PaymentMethodId);
    }

    #endregion

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_InvalidPaymentMethod_ReturnsFailed()
    {
        var context = CreatePaymentContext();
        var request = CreatePaymentRequest(100.00m, 1, "invalid");
        var result = await context.ProcessPaymentAsync("invalid_method", request);

        Assert.False(result.Success);
        Assert.Equal("Unknown payment method", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidStrategy_ReturnsSuccess()
    {
        var context = CreatePaymentContext();
        var request = CreatePaymentRequest(100.00m, 1, "credit_card");
        var result = await context.ProcessPaymentAsync("credit_card", request);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("CC-", result.TransactionId);
    }

    [Theory]
    [InlineData("credit_card", "CC-")]
    [InlineData("paypal", "PP-")]
    [InlineData("mobilepay", "MP-")]
    [InlineData("apple_pay", "AP-")]
    [InlineData("google_pay", "GP-")]
    public async Task ProcessPaymentAsync_DifferentStrategies_ReturnCorrectTransactionPrefix(
        string paymentMethodId, string expectedPrefix)
    {
        var context = CreatePaymentContext();
        var request = CreatePaymentRequest(50.00m, 123, paymentMethodId);
        var result = await context.ProcessPaymentAsync(paymentMethodId, request);

        Assert.True(result.Success);
        Assert.StartsWith(expectedPrefix, result.TransactionId);
    }

    [Fact]
    public async Task ProcessPaymentAsync_TransactionIdContainsOrderId()
    {
        var context = CreatePaymentContext();
        var orderId = 12345;
        var request = CreatePaymentRequest(100.00m, orderId, "credit_card");
        var result = await context.ProcessPaymentAsync("credit_card", request);

        Assert.Contains(orderId.ToString(), result.TransactionId);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidAmount_ReturnsFailed()
    {
        var context = CreatePaymentContext();
        var request = new PaymentRequest(0, 1, new TokenDetails("token"));
        var result = await context.ProcessPaymentAsync("mobilepay", request);

        Assert.False(result.Success);
        Assert.Equal("Amount must be greater than zero", result.ErrorMessage);
    }

    #endregion

    #region Strategy Pattern Behavior Tests

    [Fact]
    public async Task StrategyPattern_SameContext_DifferentBehavior()
    {
        // Demonstrates the Strategy Pattern: same context, different behavior based on strategy
        var context = CreatePaymentContext();

        var ccRequest = CreatePaymentRequest(100.00m, 1, "credit_card");
        var ccResult = await context.ProcessPaymentAsync("credit_card", ccRequest);

        var ppRequest = CreatePaymentRequest(100.00m, 1, "paypal");
        var ppResult = await context.ProcessPaymentAsync("paypal", ppRequest);

        // Same context, but different transaction prefixes based on strategy
        Assert.StartsWith("CC-", ccResult.TransactionId);
        Assert.StartsWith("PP-", ppResult.TransactionId);
    }

    [Fact]
    public void StrategyPattern_StrategiesAreInterchangeable()
    {
        // All strategies implement the same interface
        var context = CreatePaymentContext();
        var strategies = context.GetAvailableStrategies();

        foreach (var strategy in strategies)
        {
            Assert.IsAssignableFrom<IPaymentStrategy>(strategy);
            Assert.NotEmpty(strategy.PaymentMethodId);
            Assert.NotEmpty(strategy.DisplayName);
            Assert.NotEmpty(strategy.Description);
            Assert.NotEmpty(strategy.IconClass);
            Assert.True(strategy.IsAvailable());
        }
    }

    #endregion
}

