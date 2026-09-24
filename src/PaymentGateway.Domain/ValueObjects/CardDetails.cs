using Vogen;

namespace PaymentGateway.Domain.ValueObjects;

public record CardDetailsRecord(string CardNumber, int ExpiryMonth, int ExpiryYear, string Cvv);

[ValueObject<CardDetailsRecord>]
public readonly partial record struct CardDetails
{
    public int LastFourCardDigits => int.Parse(Value.CardNumber[^4..]);
    public CardInfo ToCardInfo => CardInfo.From(new CardInfoRecord(LastFourCardDigits, Value.ExpiryMonth, Value.ExpiryYear));
}
