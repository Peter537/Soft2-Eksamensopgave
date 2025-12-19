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
        var request = new PaymentRequest(100.00m, 1, new CreditCardDetails("4111111111111111", "12", "2027", "123"));
        var result = await strategy.ProcessPaymentAsync(request);

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
        var request = new PaymentRequest(50.00m, 2, new PayPalDetails("test@example.com"));
        var result = await strategy.ProcessPaymentAsync(request);

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
        var request = new PaymentRequest(75.50m, 3, new TokenDetails("test-token"));
        var result = await strategy.ProcessPaymentAsync(request);

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
        var request = new PaymentRequest(200.00m, 4, new TokenDetails("apple-token"));
        var result = await strategy.ProcessPaymentAsync(request);

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
        var request = new PaymentRequest(150.00m, 5, new TokenDetails("google-token"));
        var result = await strategy.ProcessPaymentAsync(request);

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

