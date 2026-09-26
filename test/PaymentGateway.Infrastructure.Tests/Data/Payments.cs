using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Infrastructure.Tests.Data;

internal static class Payments
{
    public static Payment New() => Payment.Create(
        PaymentStatus.Authorized,
        CardInfo.From(new CardInfoRecord("8877", 4, 2030)),
        Money.From(new MoneyRecord("GBP", 100)));
}
