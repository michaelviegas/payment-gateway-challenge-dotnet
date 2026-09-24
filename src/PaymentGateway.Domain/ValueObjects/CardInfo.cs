using Vogen;

namespace PaymentGateway.Domain.ValueObjects;

public record CardInfoRecord(int CardNumberLastFour, int ExpiryMonth, int ExpiryYear);

[ValueObject<CardInfoRecord>]
public readonly partial record struct CardInfo;