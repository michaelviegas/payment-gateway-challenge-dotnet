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

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"CardNumber = ****{CardNumber?[^Math.Min(4, CardNumber.Length)..]}, ");
        builder.Append($"ExpiryMonth = {ExpiryMonth}, ExpiryYear = {ExpiryYear}, Currency = {Currency}, Amount = {Amount}");
        return true;
    }
}
