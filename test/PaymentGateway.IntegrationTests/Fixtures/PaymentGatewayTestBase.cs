using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.IntegrationTests.Fixtures;

public abstract class PaymentGatewayTestBase : IDisposable
{
    private readonly PaymentGatewayFactory _factory;

    protected PaymentGatewayTestBase()
    {
        _factory = PaymentGatewayFactory.WithBank(Bank, Clock);
        Client = _factory.CreateClient();
    }

    protected FakeBankClient Bank { get; } = new();

    protected FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

    protected HttpClient Client { get; }

    protected PostPaymentRequest ValidRequest() => PaymentsApi.ValidRequest(Clock);

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
