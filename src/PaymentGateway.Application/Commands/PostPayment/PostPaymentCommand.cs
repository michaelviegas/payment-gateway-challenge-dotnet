using System.Text;

using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Commands.PostPayment;

public sealed record PostPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv) : ICommand<PostPaymentResponse>
{
    public CardDetails ToCardDetails() => CardDetails.From(new CardDetailsRecord(CardNumber, ExpiryMonth, ExpiryYear, Cvv));

    public Money ToMoney() => Money.From(new MoneyRecord(Currency, Amount));
}
