using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void AmountsInSameCurrencyAreEqual()
    {
        Assert.Equal(Money.From(new MoneyRecord("GBP", 100)), Money.From(new MoneyRecord("GBP", 100)));
    }

    [Theory]
    [InlineData("USD", 100)]
    [InlineData("GBP", 101)]
    public void DifferentCurrencyOrAmountIsNotEqual(string currency, int amount)
    {
        Assert.NotEqual(Money.From(new MoneyRecord("GBP", 100)), Money.From(new MoneyRecord(currency, amount)));
    }
}
