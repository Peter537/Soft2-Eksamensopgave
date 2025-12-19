namespace MToGo.Website.Services.Payment;

public class CreditCardPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethodId => "credit_card";
    public string DisplayName => "Credit Card";
    public string IconClass => "bi-credit-card";
    public string Description => "Pay securely with Visa, Mastercard, or American Express";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request.Details is not CreditCardDetails details)
        {
            return Task.FromResult(PaymentResult.Failed("Credit card details are required"));
        }

        var cardNumber = NormalizeDigits(details.CardNumber);
        if (cardNumber == null || cardNumber.Length is < 12 or > 19)
        {
            return Task.FromResult(PaymentResult.Failed("Invalid card number"));
        }

        if (!IsLuhnValid(cardNumber))
        {
            return Task.FromResult(PaymentResult.Failed("Invalid card number"));
        }

        var cvc = NormalizeDigits(details.Cvc);
        if (cvc == null || cvc.Length is < 3 or > 4)
        {
            return Task.FromResult(PaymentResult.Failed("Invalid CVC"));
        }

        if (!TryParseExpiry(details.ExpiryMonth, details.ExpiryYear, out var expiryUtc) || expiryUtc < DateTime.UtcNow.Date)
        {
            return Task.FromResult(PaymentResult.Failed("Card is expired"));
        }

        var transactionId = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.OrderId}";
        return Task.FromResult(PaymentResult.Successful(transactionId));
    }

    public bool IsAvailable() => true;

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new char[value.Length];
        var index = 0;

        foreach (var c in value)
        {
            if (char.IsDigit(c))
            {
                digits[index++] = c;
            }
            else if (c is ' ' or '-')
            {
                continue;
            }
            else
            {
                return null;
            }
        }

        return index == 0 ? null : new string(digits, 0, index);
    }

    private static bool IsLuhnValid(string digits)
    {
        var sum = 0;
        var alternate = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    private static bool TryParseExpiry(string month, string year, out DateTime expiryUtc)
    {
        expiryUtc = default;

        if (!int.TryParse(month, out var mm) || mm < 1 || mm > 12)
        {
            return false;
        }

        if (!int.TryParse(year, out var yy))
        {
            return false;
        }

        if (yy < 100)
        {
            yy += 2000;
        }

        if (yy is < 2000 or > 2100)
        {
            return false;
        }

        var lastDay = DateTime.DaysInMonth(yy, mm);
        expiryUtc = new DateTime(yy, mm, lastDay, 23, 59, 59, DateTimeKind.Utc);
        return true;
    }
}
