using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests;

public sealed class PaymentTests
{
    private static readonly CardInfo Card = CardInfo.From(new CardInfoRecord("8877", 4, 2030));
    private static readonly Money Money = Money.From(new MoneyRecord("GBP", 100));

    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public void CreateSetsPaymentDetails(PaymentStatus status)
    {
        var payment = Payment.Create(status, Card, Money);

        Assert.NotEqual(Guid.Empty, payment.Id.Value);
        Assert.Equal(status, payment.Status);
        Assert.Equal(Card, payment.Card);
        Assert.Equal(Money, payment.Money);
    }

    [Fact]
    public void CreateAssignsUniqueIds()
    {
        var first = Payment.Create(PaymentStatus.Authorized, Card, Money);
        var second = Payment.Create(PaymentStatus.Authorized, Card, Money);

        Assert.NotEqual(first.Id, second.Id);
    }
}
