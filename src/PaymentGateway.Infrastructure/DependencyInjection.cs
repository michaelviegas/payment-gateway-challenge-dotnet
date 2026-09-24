using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Application.Abstractions.Idempotency;
using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Bank;
using PaymentGateway.Infrastructure.Data;
using PaymentGateway.Infrastructure.Idempotency;

using Polly;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddBankClient();

        return services
            .AddSingleton<InMemoryPaymentStore>()
            .AddScoped<InMemoryDbContext>()
            .AddScoped<IAppDbContext>(provider => provider.GetRequiredService<InMemoryDbContext>())
            .AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<InMemoryDbContext>())
            .AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>()
            .AddScoped<IPaymentRepository, PaymentRepository>();
    }

    private static void AddBankClient(this IServiceCollection services)
    {
        services
            .AddHttpClient<IBankClient, BankClient>((provider, client) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Bank:BaseUrl"] ?? "http://localhost:8080");
            })
            .AddResilienceHandler("bank", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(30));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Exception is HttpRequestException
                        {
                            HttpRequestError: HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError
                        }),
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(15),
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(10));
            });
    }
}
