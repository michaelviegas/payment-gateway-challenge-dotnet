using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Application.Commands.PostPayment;

namespace PaymentGateway.Application.Tests.Commands;

public sealed class PostPaymentValidatorTests
{
    private readonly FakeTimeProvider _clock = TestCommands.Clock();
    private readonly PostPaymentValidator _validator;

    public PostPaymentValidatorTests() => _validator = new PostPaymentValidator(_clock);

    [Fact]
    public void AcceptsValidCommand()
    {
        AssertValid(TestCommands.ValidPostPayment());
    }

    [Theory]
    [InlineData("")]
    [InlineData("2222405343248")]
    [InlineData("22224053432488770000")]
    [InlineData("222240534324887a")]
    [InlineData("2222 4053 4324 8877")]
    public void RejectsInvalidCardNumber(string cardNumber)
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { CardNumber = cardNumber }, nameof(PostPaymentCommand.CardNumber));
    }

    [Fact]
    public void EmptyCardNumberReportsOnlyThatItIsRequired()
    {
        var result = _validator.Validate(TestCommands.ValidPostPayment() with { CardNumber = "" });

        var error = Assert.Single(result.Errors);
        Assert.Equal("Card number is required.", error.ErrorMessage);
    }

    [Fact]
    public void EmptyCurrencyReportsOnlyThatItIsRequired()
    {
        var result = _validator.Validate(TestCommands.ValidPostPayment() with { Currency = "" });

        var error = Assert.Single(result.Errors);
        Assert.Equal("Currency is required.", error.ErrorMessage);
    }

    [Fact]
    public void EmptyCvvReportsOnlyThatItIsRequired()
    {
        var result = _validator.Validate(TestCommands.ValidPostPayment() with { Cvv = "" });

        var error = Assert.Single(result.Errors);
        Assert.Equal("CVV is required.", error.ErrorMessage);
    }

    [Fact]
    public void RejectsNonAsciiDigitsInCardNumber()
    {
        AssertOnlyInvalid(
            TestCommands.ValidPostPayment() with { CardNumber = "٢٢٢٢405343248877" },
            nameof(PostPaymentCommand.CardNumber));
    }

    [Theory]
    [InlineData("22224053432488")]
    [InlineData("2222405343248877123")]
    public void AcceptsCardNumberLengthBoundaries(string cardNumber)
    {
        AssertValid(TestCommands.ValidPostPayment() with { CardNumber = cardNumber });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void RejectsInvalidExpiryMonth(int expiryMonth)
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { ExpiryMonth = expiryMonth }, nameof(PostPaymentCommand.ExpiryMonth));
    }

    [Fact]
    public void RejectsMissingExpiryYear()
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { ExpiryYear = 0 }, nameof(PostPaymentCommand.ExpiryYear));
    }

    [Fact]
    public void RejectsCardThatExpiredLastMonth()
    {
        var lastMonth = _clock.GetUtcNow().AddMonths(-1);

        var command = TestCommands.ValidPostPayment() with { ExpiryMonth = lastMonth.Month, ExpiryYear = lastMonth.Year };

        AssertOnlyInvalid(command, nameof(PostPaymentCommand.ExpiryYear), "Card has expired.");
    }

    [Fact]
    public void AcceptsCardExpiringThisMonth()
    {
        var now = _clock.GetUtcNow();

        AssertValid(TestCommands.ValidPostPayment() with { ExpiryMonth = now.Month, ExpiryYear = now.Year });
    }

    [Fact]
    public void RejectsCardThatExpiredInDecemberOnNewYearsDay()
    {
        _clock.SetUtcNow(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var command = TestCommands.ValidPostPayment() with { ExpiryMonth = 12, ExpiryYear = 2026 };

        AssertOnlyInvalid(command, nameof(PostPaymentCommand.ExpiryYear), "Card has expired.");
    }

    [Fact]
    public void AcceptsCardExpiringInDecemberOnNewYearsEve()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero));

        AssertValid(TestCommands.ValidPostPayment() with { ExpiryMonth = 12, ExpiryYear = 2026 });
    }

    [Theory]
    [InlineData("")]
    [InlineData("GB")]
    [InlineData("GBPS")]
    [InlineData("gbp")]
    [InlineData("JPY")]
    public void RejectsInvalidCurrency(string currency)
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { Currency = currency }, nameof(PostPaymentCommand.Currency));
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void AcceptsSupportedCurrencies(string currency)
    {
        AssertValid(TestCommands.ValidPostPayment() with { Currency = currency });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveAmount(int amount)
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { Amount = amount }, nameof(PostPaymentCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    public void RejectsInvalidCvv(string cvv)
    {
        AssertOnlyInvalid(TestCommands.ValidPostPayment() with { Cvv = cvv }, nameof(PostPaymentCommand.Cvv));
    }

    [Fact]
    public void AcceptsFourDigitCvv()
    {
        AssertValid(TestCommands.ValidPostPayment() with { Cvv = "1234" });
    }

    private void AssertValid(PostPaymentCommand command)
    {
        var result = _validator.Validate(command);

        Assert.True(result.IsValid, string.Join(", ", result.Errors));
    }

    private void AssertOnlyInvalid(PostPaymentCommand command, string property, string? message = null)
    {
        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, error => Assert.Equal(property, error.PropertyName));
        if (message is not null)
        {
            Assert.Contains(result.Errors, error => error.ErrorMessage == message);
        }
    }
}
