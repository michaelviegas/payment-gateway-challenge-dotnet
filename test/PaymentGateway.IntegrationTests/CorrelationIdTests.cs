using PaymentGateway.Api.Middleware;
using PaymentGateway.IntegrationTests.Fixtures;

namespace PaymentGateway.IntegrationTests;

public sealed class CorrelationIdTests : PaymentGatewayTestBase
{
    [Fact]
    public async Task EchoesProvidedCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Payments/{Guid.NewGuid()}");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "correlation-123");

        var response = await Client.SendAsync(request);

        Assert.Equal("correlation-123", Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
    }

    [Fact]
    public async Task GeneratesCorrelationIdWhenNoneProvided()
    {
        var response = await Client.GetPaymentAsync(Guid.NewGuid());

        var correlationId = Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName));
        Assert.True(Guid.TryParse(correlationId, out _));
    }
}
