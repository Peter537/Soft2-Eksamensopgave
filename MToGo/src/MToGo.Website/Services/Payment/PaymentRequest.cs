namespace MToGo.Website.Services.Payment;

public sealed record PaymentRequest(
    decimal Amount,
    int OrderId,
    PaymentDetails Details);
