using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;
using PaymentGateway.Infrastructure.Bank;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PaymentGateway.Infrastructure.Tests.Bank;

public sealed class BankClientTests
{
    private const string AuthorizedJson = """{"authorized":true,"authorization_code":"0bb07405-6d44-4b50-a14f-7ae0beff13ad"}""";
    private const string DeclinedJson = """{"authorized":false,"authorization_code":""}""";

    private static readonly CardDetails Card = CardDetails.From(new CardDetailsRecord("2222405343248877", 4, 2030, "123"));
    private static readonly Money Money = Money.From(new MoneyRecord("GBP", 100));

    [Fact]
    public async Task PostsPaymentInBankFormat()
    {
        var handler = StubHttpHandler.Returning(HttpStatusCode.OK, AuthorizedJson);

        await AuthorizeAsync(handler);

        var (request, body) = Assert.Single(handler.Requests);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://bank.test/payments", request.RequestUri!.ToString());
        Assert.Equal("2222405343248877", root.GetProperty("card_number").GetString());
        Assert.Equal("04/2030", root.GetProperty("expiry_date").GetString());
        Assert.Equal("GBP", root.GetProperty("currency").GetString());
        Assert.Equal(100, root.GetProperty("amount").GetInt32());
        Assert.Equal("123", root.GetProperty("cvv").GetString());
    }

    [Fact]
    public async Task AuthorizedResponseCarriesAuthorizationCode()
    {
        var result = await AuthorizeAsync(StubHttpHandler.Returning(HttpStatusCode.OK, AuthorizedJson));

        Assert.Equal(BankAuthorizationResult.Authorized("0bb07405-6d44-4b50-a14f-7ae0beff13ad"), result);
    }

    [Fact]
    public async Task DeclinedResponseIsDeclined()
    {
        var result = await AuthorizeAsync(StubHttpHandler.Returning(HttpStatusCode.OK, DeclinedJson));

        Assert.Equal(PaymentStatus.Declined, result.PaymentStatus);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ErrorStatusIsRejected(HttpStatusCode statusCode)
    {
        var result = await AuthorizeAsync(StubHttpHandler.Returning(statusCode, AuthorizedJson));

        Assert.True(result.PaymentRejected);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task UnreadableBodyIsRejected(string body)
    {
        var result = await AuthorizeAsync(StubHttpHandler.Returning(HttpStatusCode.OK, body));

        Assert.True(result.PaymentRejected);
    }

    public static TheoryData<Exception> HandledFailures => new()
    {
        new HttpRequestException(HttpRequestError.ConnectionError),
        new HttpRequestException(HttpRequestError.NameResolutionError),
        new HttpRequestException(HttpRequestError.ResponseEnded),
        new BrokenCircuitException(),
        new TimeoutRejectedException(),
        new TaskCanceledException()
    };

    [Theory]
    [MemberData(nameof(HandledFailures))]
    public async Task TransportFailureIsRejected(Exception failure)
    {
        var result = await AuthorizeAsync(StubHttpHandler.Throwing(failure));

        Assert.True(result.PaymentRejected);
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var handler = StubHttpHandler.Returning(HttpStatusCode.OK, AuthorizedJson);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuthorizeAsync(handler, cancellation.Token));
    }

    [Fact]
    public async Task UnexpectedExceptionPropagates()
    {
        var handler = StubHttpHandler.Throwing(new InvalidOperationException("Simulated failure."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => AuthorizeAsync(handler));
    }

    private static async Task<BankAuthorizationResult> AuthorizeAsync(
        HttpMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bank.test") };
        var client = new BankClient(httpClient, NullLogger<BankClient>.Instance);

        return await client.AuthorizeAsync(Card, Money, cancellationToken);
    }
}
