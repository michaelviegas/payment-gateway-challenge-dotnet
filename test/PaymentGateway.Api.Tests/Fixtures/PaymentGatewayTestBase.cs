namespace PaymentGateway.Api.Tests.Fixtures;

public abstract class PaymentGatewayTestBase : IDisposable
{
    private readonly PaymentGatewayFactory _factory;

    protected PaymentGatewayTestBase()
    {
        _factory = PaymentGatewayFactory.WithBank(Bank);
        Client = _factory.CreateClient();
    }

    protected FakeBankClient Bank { get; } = new();

    protected HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
