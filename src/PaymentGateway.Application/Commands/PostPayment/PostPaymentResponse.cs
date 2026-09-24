using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Commands.PostPayment;

public sealed record PostPaymentResponse
{
    public Guid Id { get; init; }
    public PaymentStatus Status { get; init; }
    public int CardNumberLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public int Amount { get; init; }

    public static PostPaymentResponse Create(Payment payment) => new()
    {
        Id = payment.Id.Value,
        Status = payment.Status,
        CardNumberLastFour = payment.Card.Value.CardNumberLastFour,
        ExpiryMonth = payment.Card.Value.ExpiryMonth,
        ExpiryYear = payment.Card.Value.ExpiryYear,
        Currency = payment.Money.Value.Currency,
        Amount = payment.Money.Value.Amount
    };
}
