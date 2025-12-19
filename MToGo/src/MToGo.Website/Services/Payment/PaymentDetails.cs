namespace MToGo.Website.Services.Payment;

public abstract record PaymentDetails;

public sealed record CreditCardDetails(
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvc) : PaymentDetails;

public sealed record TokenDetails(string Token) : PaymentDetails;

public sealed record PayPalDetails(string Email) : PaymentDetails;
