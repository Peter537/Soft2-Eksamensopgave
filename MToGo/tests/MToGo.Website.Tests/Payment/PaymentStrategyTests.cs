using MToGo.Website.Services.Payment;

namespace MToGo.Website.Tests.Payment;

public class PaymentStrategyTests
{
    #region CreditCardPaymentStrategy Tests

    [Fact]
    public void CreditCardPaymentStrategy_HasCorrectPaymentMethodId()
    {
        var strategy = new CreditCardPaymentStrategy();
        Assert.Equal("credit_card", strategy.PaymentMethodId);
    }

    [Fact]
    public void CreditCardPaymentStrategy_HasCorrectDisplayName()
    {
        var strategy = new CreditCardPaymentStrategy();
        Assert.Equal("Credit Card", strategy.DisplayName);
    }

    [Fact]
    public void CreditCardPaymentStrategy_IsAvailable()
    {
        var strategy = new CreditCardPaymentStrategy();
        Assert.True(strategy.IsAvailable());
    }

    [Fact]
    public async Task CreditCardPaymentStrategy_ProcessPayment_ReturnsSuccess()
    {
        var strategy = new CreditCardPaymentStrategy();
        var result = await strategy.ProcessPaymentAsync(100.00m, 1);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("CC-", result.TransactionId);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region PayPalPaymentStrategy Tests

    [Fact]
    public void PayPalPaymentStrategy_HasCorrectPaymentMethodId()
    {
        var strategy = new PayPalPaymentStrategy();
        Assert.Equal("paypal", strategy.PaymentMethodId);
    }

    [Fact]
    public void PayPalPaymentStrategy_HasCorrectDisplayName()
    {
        var strategy = new PayPalPaymentStrategy();
        Assert.Equal("PayPal", strategy.DisplayName);
    }

    [Fact]
    public void PayPalPaymentStrategy_IsAvailable()
    {
        var strategy = new PayPalPaymentStrategy();
        Assert.True(strategy.IsAvailable());
    }

    [Fact]
    public async Task PayPalPaymentStrategy_ProcessPayment_ReturnsSuccess()
    {
        var strategy = new PayPalPaymentStrategy();
        var result = await strategy.ProcessPaymentAsync(50.00m, 2);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("PP-", result.TransactionId);
    }

    #endregion

    #region MobilePayPaymentStrategy Tests

    [Fact]
    public void MobilePayPaymentStrategy_HasCorrectPaymentMethodId()
    {
        var strategy = new MobilePayPaymentStrategy();
        Assert.Equal("mobilepay", strategy.PaymentMethodId);
    }

    [Fact]
    public void MobilePayPaymentStrategy_HasCorrectDisplayName()
    {
        var strategy = new MobilePayPaymentStrategy();
        Assert.Equal("MobilePay", strategy.DisplayName);
    }

    [Fact]
    public void MobilePayPaymentStrategy_IsAvailable()
    {
        var strategy = new MobilePayPaymentStrategy();
        Assert.True(strategy.IsAvailable());
    }

    [Fact]
    public async Task MobilePayPaymentStrategy_ProcessPayment_ReturnsSuccess()
    {
        var strategy = new MobilePayPaymentStrategy();
        var result = await strategy.ProcessPaymentAsync(75.50m, 3);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("MP-", result.TransactionId);
    }

    #endregion

    #region ApplePayPaymentStrategy Tests

    [Fact]
    public void ApplePayPaymentStrategy_HasCorrectPaymentMethodId()
    {
        var strategy = new ApplePayPaymentStrategy();
        Assert.Equal("apple_pay", strategy.PaymentMethodId);
    }

    [Fact]
    public void ApplePayPaymentStrategy_HasCorrectDisplayName()
    {
        var strategy = new ApplePayPaymentStrategy();
        Assert.Equal("Apple Pay", strategy.DisplayName);
    }

    [Fact]
    public void ApplePayPaymentStrategy_IsAvailable()
    {
        var strategy = new ApplePayPaymentStrategy();
        Assert.True(strategy.IsAvailable());
    }

    [Fact]
    public async Task ApplePayPaymentStrategy_ProcessPayment_ReturnsSuccess()
    {
        var strategy = new ApplePayPaymentStrategy();
        var result = await strategy.ProcessPaymentAsync(200.00m, 4);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("AP-", result.TransactionId);
    }

    #endregion

    #region GooglePayPaymentStrategy Tests

    [Fact]
    public void GooglePayPaymentStrategy_HasCorrectPaymentMethodId()
    {
        var strategy = new GooglePayPaymentStrategy();
        Assert.Equal("google_pay", strategy.PaymentMethodId);
    }

    [Fact]
    public void GooglePayPaymentStrategy_HasCorrectDisplayName()
    {
        var strategy = new GooglePayPaymentStrategy();
        Assert.Equal("Google Pay", strategy.DisplayName);
    }

    [Fact]
    public void GooglePayPaymentStrategy_IsAvailable()
    {
        var strategy = new GooglePayPaymentStrategy();
        Assert.True(strategy.IsAvailable());
    }

    [Fact]
    public async Task GooglePayPaymentStrategy_ProcessPayment_ReturnsSuccess()
    {
        var strategy = new GooglePayPaymentStrategy();
        var result = await strategy.ProcessPaymentAsync(150.00m, 5);

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("GP-", result.TransactionId);
    }

    #endregion

    #region All Strategies - Common Interface Tests

    [Theory]
    [InlineData(typeof(CreditCardPaymentStrategy))]
    [InlineData(typeof(PayPalPaymentStrategy))]
    [InlineData(typeof(MobilePayPaymentStrategy))]
    [InlineData(typeof(ApplePayPaymentStrategy))]
    [InlineData(typeof(GooglePayPaymentStrategy))]
    public void AllStrategies_ImplementIPaymentStrategy(Type strategyType)
    {
        var strategy = Activator.CreateInstance(strategyType);
        Assert.IsAssignableFrom<IPaymentStrategy>(strategy);
    }

    [Theory]
    [InlineData(typeof(CreditCardPaymentStrategy))]
    [InlineData(typeof(PayPalPaymentStrategy))]
    [InlineData(typeof(MobilePayPaymentStrategy))]
    [InlineData(typeof(ApplePayPaymentStrategy))]
    [InlineData(typeof(GooglePayPaymentStrategy))]
    public void AllStrategies_HaveNonEmptyDisplayName(Type strategyType)
    {
        var strategy = (IPaymentStrategy)Activator.CreateInstance(strategyType)!;
        Assert.False(string.IsNullOrWhiteSpace(strategy.DisplayName));
    }

    [Theory]
    [InlineData(typeof(CreditCardPaymentStrategy))]
    [InlineData(typeof(PayPalPaymentStrategy))]
    [InlineData(typeof(MobilePayPaymentStrategy))]
    [InlineData(typeof(ApplePayPaymentStrategy))]
    [InlineData(typeof(GooglePayPaymentStrategy))]
    public void AllStrategies_HaveNonEmptyDescription(Type strategyType)
    {
        var strategy = (IPaymentStrategy)Activator.CreateInstance(strategyType)!;
        Assert.False(string.IsNullOrWhiteSpace(strategy.Description));
    }

    [Theory]
    [InlineData(typeof(CreditCardPaymentStrategy))]
    [InlineData(typeof(PayPalPaymentStrategy))]
    [InlineData(typeof(MobilePayPaymentStrategy))]
    [InlineData(typeof(ApplePayPaymentStrategy))]
    [InlineData(typeof(GooglePayPaymentStrategy))]
    public void AllStrategies_HaveIconClass(Type strategyType)
    {
        var strategy = (IPaymentStrategy)Activator.CreateInstance(strategyType)!;
        Assert.StartsWith("bi-", strategy.IconClass);
    }

    #endregion
}
