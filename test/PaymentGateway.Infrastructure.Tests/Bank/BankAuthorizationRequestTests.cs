using PaymentGateway.Domain.ValueObjects;
using PaymentGateway.Infrastructure.Bank.DTOs;

namespace PaymentGateway.Infrastructure.Tests.Bank;

public sealed class BankAuthorizationRequestTests
{
    [Theory]
    [InlineData(1, 2030, "01/2030")]
    [InlineData(12, 2031, "12/2031")]
    public void FormatsExpiryDateAsMonthSlashYear(int month, int year, string expected)
    {
        var request = Create(CardDetails.From(new CardDetailsRecord("2222405343248877", month, year, "123")));

        Assert.Equal(expected, request.ExpiryDate);
    }

    [Fact]
    public void CopiesCardAndMoney()
    {
        var request = Create(CardDetails.From(new CardDetailsRecord("2222405343248877", 4, 2030, "1234")));

        Assert.Equal("2222405343248877", request.CardNumber);
        Assert.Equal("1234", request.Cvv);
        Assert.Equal("EUR", request.Currency);
        Assert.Equal(250, request.Amount);
    }

    private static BankAuthorizationRequest Create(CardDetails card) =>
        BankAuthorizationRequest.Create(card, Money.From(new MoneyRecord("EUR", 250)));
}
