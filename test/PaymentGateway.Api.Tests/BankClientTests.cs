using System.Net;
using System.Text.Json;

using PaymentGateway.Api.Tests.Fixtures;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests;

public sealed class BankClientTests
{
    private const string AuthorizedJson = """{"authorized":true,"authorization_code":"0bb07405-6d44-4b50-a14f-7ae0beff13ad"}""";
    private const string DeclinedJson = """{"authorized":false,"authorization_code":""}""";

    [Fact]
    public async Task SendsPaymentInBankFormat()
    {
        var bank = StubBankHandler.Returning(HttpStatusCode.OK, AuthorizedJson);
        var request = PaymentsApi.ValidRequest();

        await PostPaymentAsync(bank);

        var (message, body) = Assert.Single(bank.Requests);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal(HttpMethod.Post, message.Method);
        Assert.Equal("/payments", message.RequestUri!.AbsolutePath);
        Assert.Equal(request.CardNumber, root.GetProperty("card_number").GetString());
        Assert.Equal($"04/{request.ExpiryYear}", root.GetProperty("expiry_date").GetString());
        Assert.Equal(request.Currency, root.GetProperty("currency").GetString());
        Assert.Equal(request.Amount, root.GetProperty("amount").GetInt32());
        Assert.Equal(request.Cvv, root.GetProperty("cvv").GetString());
    }

    [Theory]
    [InlineData(AuthorizedJson, PaymentStatus.Authorized)]
    [InlineData(DeclinedJson, PaymentStatus.Declined)]
    public async Task MapsBankDecisionToPaymentStatus(string bankResponse, PaymentStatus expectedStatus)
    {
        var response = await PostPaymentAsync(StubBankHandler.Returning(HttpStatusCode.OK, bankResponse));
        var payment = await response.ReadAsAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedStatus, payment.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task BankErrorStatusRejectsPayment(HttpStatusCode bankStatus)
    {
        var response = await PostPaymentAsync(StubBankHandler.Returning(bankStatus));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task UnreadableBankResponseRejectsPayment(string bankResponse)
    {
        var response = await PostPaymentAsync(StubBankHandler.Returning(HttpStatusCode.OK, bankResponse));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConnectionDroppedMidRequestRejectsPaymentWithoutRetrying()
    {
        var bank = StubBankHandler.Throwing(new HttpRequestException(HttpRequestError.ResponseEnded));

        var response = await PostPaymentAsync(bank);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(bank.Requests);
    }

    [Fact]
    public async Task UnreachableBankIsRetriedThenRejected()
    {
        var bank = StubBankHandler.Throwing(new HttpRequestException(HttpRequestError.ConnectionError));

        var response = await PostPaymentAsync(bank);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(4, bank.Requests.Count);
    }

    private static async Task<HttpResponseMessage> PostPaymentAsync(StubBankHandler bank)
    {
        using var factory = PaymentGatewayFactory.WithBankHandler(bank);
        using var client = factory.CreateClient();

        var response = await client.PostPaymentAsync(PaymentsApi.ValidRequest());
        await response.Content.LoadIntoBufferAsync();

        return response;
    }
}
