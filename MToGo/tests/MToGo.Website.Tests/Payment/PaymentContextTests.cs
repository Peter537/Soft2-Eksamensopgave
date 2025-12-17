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

    #region SetStrategy Tests

    [Fact]
    public void SetStrategy_ValidId_ReturnsTrue()
    {
        var context = CreatePaymentContext();
        var result = context.SetStrategy("paypal");

        Assert.True(result);
    }

    [Fact]
    public void SetStrategy_InvalidId_ReturnsFalse()
    {
        var context = CreatePaymentContext();
        var result = context.SetStrategy("invalid_method");

        Assert.False(result);
    }

    [Fact]
    public void SetStrategy_ValidId_SetsCurrentStrategy()
    {
        var context = CreatePaymentContext();
        context.SetStrategy("mobilepay");

        Assert.NotNull(context.CurrentStrategy);
        Assert.Equal("mobilepay", context.CurrentStrategy.PaymentMethodId);
    }

    [Fact]
    public void SetStrategy_CanSwitchStrategies()
    {
        var context = CreatePaymentContext();

        context.SetStrategy("credit_card");
        Assert.Equal("credit_card", context.CurrentStrategy?.PaymentMethodId);

        context.SetStrategy("paypal");
        Assert.Equal("paypal", context.CurrentStrategy?.PaymentMethodId);

        context.SetStrategy("google_pay");
        Assert.Equal("google_pay", context.CurrentStrategy?.PaymentMethodId);
    }

    #endregion

    #region CurrentStrategy Tests

    [Fact]
    public void CurrentStrategy_InitiallyNull()
    {
        var context = CreatePaymentContext();
        Assert.Null(context.CurrentStrategy);
    }

    [Fact]
    public void CurrentStrategy_AfterSetStrategy_IsSet()
    {
        var context = CreatePaymentContext();
        context.SetStrategy("apple_pay");

        Assert.NotNull(context.CurrentStrategy);
        Assert.IsType<ApplePayPaymentStrategy>(context.CurrentStrategy);
    }

    #endregion

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_NoStrategySelected_ReturnsFailed()
    {
        var context = CreatePaymentContext();
        var result = await context.ProcessPaymentAsync(100.00m, 1);

        Assert.False(result.Success);
        Assert.Equal("No payment method selected", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithStrategy_ReturnsSuccess()
    {
        var context = CreatePaymentContext();
        context.SetStrategy("credit_card");

        var result = await context.ProcessPaymentAsync(100.00m, 1);

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
        context.SetStrategy(paymentMethodId);

        var result = await context.ProcessPaymentAsync(50.00m, 123);

        Assert.True(result.Success);
        Assert.StartsWith(expectedPrefix, result.TransactionId);
    }

    [Fact]
    public async Task ProcessPaymentAsync_TransactionIdContainsOrderId()
    {
        var context = CreatePaymentContext();
        context.SetStrategy("credit_card");

        var orderId = 12345;
        var result = await context.ProcessPaymentAsync(100.00m, orderId);

        Assert.Contains(orderId.ToString(), result.TransactionId);
    }

    #endregion

    #region Strategy Pattern Behavior Tests

    [Fact]
    public async Task StrategyPattern_SameContext_DifferentBehavior()
    {
        // Demonstrates the Strategy Pattern: same context, different behavior based on strategy
        var context = CreatePaymentContext();

        context.SetStrategy("credit_card");
        var ccResult = await context.ProcessPaymentAsync(100.00m, 1);

        context.SetStrategy("paypal");
        var ppResult = await context.ProcessPaymentAsync(100.00m, 1);

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

