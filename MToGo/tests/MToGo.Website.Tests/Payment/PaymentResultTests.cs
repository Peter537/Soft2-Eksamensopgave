using MToGo.Website.Services.Payment;

namespace MToGo.Website.Tests.Payment;

public class PaymentResultTests
{
    [Fact]
    public void Successful_CreatesSuccessResult()
    {
        var transactionId = "TEST-123";
        var result = PaymentResult.Successful(transactionId);

        Assert.True(result.Success);
        Assert.Equal(transactionId, result.TransactionId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        var errorMessage = "Payment declined";
        var result = PaymentResult.Failed(errorMessage);

        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void ProcessedAt_IsSetToCurrentTime()
    {
        var before = DateTime.UtcNow;
        var result = PaymentResult.Successful("TEST");
        var after = DateTime.UtcNow;

        Assert.InRange(result.ProcessedAt, before, after);
    }

    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var result = new PaymentResult();

        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var result = new PaymentResult
        {
            Success = true,
            TransactionId = "CUSTOM-ID",
            ErrorMessage = null,
            ProcessedAt = DateTime.UtcNow
        };

        Assert.True(result.Success);
        Assert.Equal("CUSTOM-ID", result.TransactionId);
    }
}

