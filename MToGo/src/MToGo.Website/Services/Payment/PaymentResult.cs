namespace MToGo.Website.Services.Payment;

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public static PaymentResult Successful(string transactionId) => new()
    {
        Success = true,
        TransactionId = transactionId
    };

    public static PaymentResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
