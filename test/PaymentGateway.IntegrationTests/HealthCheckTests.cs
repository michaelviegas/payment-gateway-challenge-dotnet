using System.Net;

using PaymentGateway.IntegrationTests.Fixtures;

namespace PaymentGateway.IntegrationTests;

public sealed class HealthCheckTests
{
    [Fact]
    public async Task LivenessIsHealthyEvenWhenBankIsDown()
    {
        var (status, body) = await GetAsync("/health/live", UnreachableBank());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task ReadinessIsHealthyWhenBankAnswers()
    {
        // The simulator answers unknown routes with 400; any response proves the bank is reachable.
        var (status, body) = await GetAsync("/health/ready", StubBankHandler.Returning(HttpStatusCode.BadRequest));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task ReadinessIsDegradedButStillServingWhenBankIsUnreachable()
    {
        var (status, body) = await GetAsync("/health/ready", UnreachableBank());

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Degraded", body);
    }

    [Fact]
    public async Task ReadinessProbeDoesNotCallThePaymentsEndpoint()
    {
        var bank = StubBankHandler.Returning(HttpStatusCode.BadRequest);

        await GetAsync("/health/ready", bank);

        var (request, _) = Assert.Single(bank.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.NotEqual("/payments", request.RequestUri!.AbsolutePath);
    }

    private static StubBankHandler UnreachableBank() =>
        StubBankHandler.Throwing(new HttpRequestException(HttpRequestError.ConnectionError));

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(string path, StubBankHandler bank)
    {
        using var factory = PaymentGatewayFactory.WithBankHandler(bank);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}
