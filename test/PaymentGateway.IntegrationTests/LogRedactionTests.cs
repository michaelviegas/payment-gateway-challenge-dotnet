using System.Net;
using System.Text;

using PaymentGateway.IntegrationTests.Fixtures;

namespace PaymentGateway.IntegrationTests;

/// <summary>
/// Card data must never reach the logs (PCI DSS). Each scenario drives a different logging path
/// (success, bank failures, validation, unreadable body) and checks the output as it would be shipped.
/// </summary>
public sealed class LogRedactionTests
{
    private const string CardNumber = "2222405343248877";
    private const string AuthorizedJson = """{"authorized":true,"authorization_code":"0bb07405-6d44-4b50-a14f-7ae0beff13ad"}""";

    private static readonly int ValidYear = DateTime.UtcNow.Year + 1;

    public static TheoryData<string> Scenarios => new()
    {
        "authorized",
        "bank-error-status",
        "bank-unreachable",
        "bank-dropped-connection",
        "bank-unreadable-body",
        "invalid-request",
        "malformed-json"
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task FullCardNumberAndCvvNeverReachTheLogs(string scenario)
    {
        var logs = new LogCapture();
        using var factory = PaymentGatewayFactory.WithBankHandler(BankFor(scenario), logs);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/Payments",
            new StringContent(BodyFor(scenario), Encoding.UTF8, "application/json"));

        var rendered = logs.Render();
        Assert.NotEmpty(logs.Events);
        Assert.DoesNotContain(CardNumber, rendered);
        Assert.DoesNotContain(CardNumber[..12], rendered);
        Assert.DoesNotMatch("(?i)\"cvv\"\\s*:", rendered);
    }

    private static StubBankHandler BankFor(string scenario) => scenario switch
    {
        "bank-error-status" => StubBankHandler.Returning(HttpStatusCode.ServiceUnavailable),
        "bank-unreachable" => StubBankHandler.Throwing(new HttpRequestException(HttpRequestError.ConnectionError)),
        "bank-dropped-connection" => StubBankHandler.Throwing(new HttpRequestException(HttpRequestError.ResponseEnded)),
        "bank-unreadable-body" => StubBankHandler.Returning(HttpStatusCode.OK, "not json"),
        _ => StubBankHandler.Returning(HttpStatusCode.OK, AuthorizedJson)
    };

    private static string BodyFor(string scenario) => scenario switch
    {
        "invalid-request" => Body(expiryYear: 2000, amount: "100"),
        "malformed-json" => Body(expiryYear: ValidYear, amount: "10.5"),
        _ => Body(expiryYear: ValidYear, amount: "100")
    };

    private static string Body(int expiryYear, string amount) =>
        $$"""{"cardNumber":"{{CardNumber}}","expiryMonth":4,"expiryYear":{{expiryYear}},"currency":"GBP","amount":{{amount}},"cvv":"123"}""";
}
