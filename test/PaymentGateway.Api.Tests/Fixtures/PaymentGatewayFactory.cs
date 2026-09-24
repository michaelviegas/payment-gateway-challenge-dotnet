using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Application.Abstractions.Bank;

namespace PaymentGateway.Api.Tests.Fixtures;

public sealed class PaymentGatewayFactory : WebApplicationFactory<PaymentsController>
{
    private readonly Action<IServiceCollection> _configureBank;

    private PaymentGatewayFactory(Action<IServiceCollection> configureBank) => _configureBank = configureBank;

    public static PaymentGatewayFactory WithBank(IBankClient bank) => new(services =>
    {
        services.RemoveAll<IBankClient>();
        services.AddSingleton(bank);
    });

    public static PaymentGatewayFactory WithBankHandler(HttpMessageHandler handler) => new(services =>
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => handler)));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(_configureBank);
    }
}
