using Vogen;

namespace PaymentGateway.Domain.ValueObjects;

public record CardInfoRecord(string CardNumberLastFour, int ExpiryMonth, int ExpiryYear);

[ValueObject<CardInfoRecord>]
public readonly partial record struct CardInfo;