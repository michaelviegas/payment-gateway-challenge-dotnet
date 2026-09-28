using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.IntegrationTests.Fixtures;

namespace PaymentGateway.IntegrationTests;

public sealed class PostPaymentValidationTests : PaymentGatewayTestBase
{
    [Theory]
    [InlineData("")]
    [InlineData("2222405343248")]
    [InlineData("22224053432488770000")]
    [InlineData("222240534324887a")]
    [InlineData("2222 4053 4324 8877")]
    public async Task RejectsInvalidCardNumber(string cardNumber)
    {
        await AssertRejectedAsync(request => request.CardNumber = cardNumber, nameof(PostPaymentRequest.CardNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task RejectsInvalidExpiryMonth(int expiryMonth)
    {
        await AssertRejectedAsync(request => request.ExpiryMonth = expiryMonth, nameof(PostPaymentRequest.ExpiryMonth));
    }

    [Fact]
    public async Task RejectsMissingExpiryYear()
    {
        await AssertRejectedAsync(request => request.ExpiryYear = 0, nameof(PostPaymentRequest.ExpiryYear));
    }

    [Fact]
    public async Task RejectsCardThatExpiredLastMonth()
    {
        var lastMonth = Clock.GetUtcNow().AddMonths(-1);

        await AssertRejectedAsync(
            request =>
            {
                request.ExpiryMonth = lastMonth.Month;
                request.ExpiryYear = lastMonth.Year;
            },
            nameof(PostPaymentRequest.ExpiryYear));
    }

    [Theory]
    [InlineData("")]
    [InlineData("GB")]
    [InlineData("GBPS")]
    [InlineData("gbp")]
    [InlineData("JPY")]
    public async Task RejectsInvalidCurrency(string currency)
    {
        await AssertRejectedAsync(request => request.Currency = currency, nameof(PostPaymentRequest.Currency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RejectsNonPositiveAmount(int amount)
    {
        await AssertRejectedAsync(request => request.Amount = amount, nameof(PostPaymentRequest.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    public async Task RejectsInvalidCvv(string cvv)
    {
        await AssertRejectedAsync(request => request.Cvv = cvv, nameof(PostPaymentRequest.Cvv));
    }

    [Fact]
    public async Task ReportsEveryInvalidField()
    {
        var request = new PostPaymentRequest();

        var response = await Client.PostPaymentAsync(request);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ["Amount", "CardNumber", "Currency", "Cvv", "ExpiryMonth", "ExpiryYear"],
            problem.Errors.Keys.Order());
    }

    [Theory]
    [InlineData("""{"amount":10.5}""", "$.amount")]
    [InlineData("""{"expiryMonth":"April"}""", "$.expiryMonth")]
    [InlineData("""{"cardNumber":2222405343248877}""", "$.cardNumber")]
    [InlineData("not json", "$")]
    [InlineData("", "")]
    public async Task RejectsBodyThatCannotBeRead(string json, string invalidField)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/api/Payments", content);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(invalidField, problem.Errors.Keys);
        Assert.All(problem.Errors.Values.SelectMany(messages => messages), message => Assert.DoesNotContain("System.", message));
        Assert.Empty(Bank.Calls);
    }

    [Fact]
    public async Task AcceptsCardExpiringThisMonth()
    {
        var now = Clock.GetUtcNow();

        await AssertAcceptedAsync(request =>
        {
            request.ExpiryMonth = now.Month;
            request.ExpiryYear = now.Year;
        });
    }

    [Theory]
    [InlineData("22224053432488")]
    [InlineData("2222405343248877123")]
    public async Task AcceptsCardNumberLengthBoundaries(string cardNumber)
    {
        await AssertAcceptedAsync(request => request.CardNumber = cardNumber);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public async Task AcceptsSupportedCurrencies(string currency)
    {
        await AssertAcceptedAsync(request => request.Currency = currency);
    }

    [Fact]
    public async Task AcceptsFourDigitCvv()
    {
        await AssertAcceptedAsync(request => request.Cvv = "1234");
    }

    private async Task AssertRejectedAsync(Action<PostPaymentRequest> configure, string invalidField)
    {
        var request = ValidRequest();
        configure(request);

        var response = await Client.PostPaymentAsync(request);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Payment.Rejected", problem.Title);
        Assert.Equal([invalidField], problem.Errors.Keys);
        Assert.Empty(Bank.Calls);
    }

    private async Task AssertAcceptedAsync(Action<PostPaymentRequest> configure)
    {
        var request = ValidRequest();
        configure(request);

        var response = await Client.PostPaymentAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(Bank.Calls);
    }
}
