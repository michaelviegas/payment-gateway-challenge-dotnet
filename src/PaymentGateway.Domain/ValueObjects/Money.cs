using Vogen;

namespace PaymentGateway.Domain.ValueObjects;

public record MoneyRecord(string Currency, int Amount);

[ValueObject<MoneyRecord>]
public readonly partial record struct Money;