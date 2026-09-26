using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Application.Abstractions.Bank;

using Serilog.Core;

namespace PaymentGateway.IntegrationTests.Fixtures;

public sealed class PaymentGatewayFactory : WebApplicationFactory<PaymentsController>
{
    private readonly Action<IServiceCollection> _configureBank;

    private PaymentGatewayFactory(Action<IServiceCollection> configureBank) => _configureBank = configureBank;

    public static PaymentGatewayFactory WithBank(IBankClient bank, TimeProvider clock) => new(services =>
    {
        services.RemoveAll<IBankClient>();
        services.AddSingleton(bank);
        services.RemoveAll<TimeProvider>();
        services.AddSingleton(clock);
    });

    /// <summary>Keeps the real bank client and resilience pipeline, and stubs the network underneath.</summary>
    /// <param name="logs">Optional Serilog sink that receives every event the app logs.</param>
    public static PaymentGatewayFactory WithBankHandler(HttpMessageHandler handler, ILogEventSink? logs = null) => new(services =>
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => handler));

        if (logs is not null)
        {
            services.AddSingleton(logs);
        }
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(_configureBank);
    }
}
