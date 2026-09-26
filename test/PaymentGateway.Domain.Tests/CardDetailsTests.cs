using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests;

public sealed class CardDetailsTests
{
    [Theory]
    [InlineData("2222405343248877", "8877")]
    [InlineData("22224053432488", "2488")]
    [InlineData("2222405343248877123", "7123")]
    [InlineData("2222405343240012", "0012")]
    public void LastFourCardDigitsReadsEndOfCardNumber(string cardNumber, string expected)
    {
        var card = CardDetails.From(new CardDetailsRecord(cardNumber, 4, 2030, "123"));

        Assert.Equal(expected, card.LastFourCardDigits);
    }

    [Fact]
    public void ToCardInfoKeepsOnlyNonSensitiveDetails()
    {
        var card = CardDetails.From(new CardDetailsRecord("2222405343248877", 4, 2030, "123"));

        var info = card.ToCardInfo;

        Assert.Equal(new CardInfoRecord("8877", 4, 2030), info.Value);
    }

    [Fact]
    public void CardsWithSameDetailsAreEqual()
    {
        var first = CardDetails.From(new CardDetailsRecord("2222405343248877", 4, 2030, "123"));
        var second = CardDetails.From(new CardDetailsRecord("2222405343248877", 4, 2030, "123"));

        Assert.Equal(first, second);
    }
}
