using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Commands.PostPayment;

public sealed record PostPaymentCommand(
    string IdempotencyKey,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv) : ICommand<PostPaymentResponse>, IIdempotentCommand
{
    public CardDetails CardDetails => CardDetails.From(new CardDetailsRecord(CardNumber, ExpiryMonth, ExpiryYear, Cvv));

    public Money Money => Money.From(new MoneyRecord(Currency, Amount));
}
