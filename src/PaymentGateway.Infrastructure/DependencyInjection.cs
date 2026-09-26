using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Bank;
using PaymentGateway.Infrastructure.Data;

using Polly;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Health check tag for dependencies that decide whether the instance is ready for traffic.</summary>
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddBankClient();

        return services
            .AddSingleton<InMemoryPaymentStore>()
            .AddScoped<InMemoryDbContext>()
            .AddScoped<IAppDbContext>(provider => provider.GetRequiredService<InMemoryDbContext>())
            .AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<InMemoryDbContext>())
            .AddScoped<IPaymentRepository, PaymentRepository>();
    }

    private static void AddBankClient(this IServiceCollection services)
    {
        // A missing or malformed bank URL stops the app at startup instead of failing the first payment.
        services
            .AddOptions<BankOptions>()
            .BindConfiguration(BankOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.AttemptTimeout > TimeSpan.Zero && options.AttemptTimeout <= options.TotalTimeout,
                "Bank:AttemptTimeout must be positive and no longer than Bank:TotalTimeout.")
            .ValidateOnStart();

        services
            .AddHttpClient<IBankClient, BankClient>((provider, client) =>
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BankOptions>>().Value.BaseUrl))
            .AddResilienceHandler("bank", (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<BankOptions>>().Value;

                pipeline.AddTimeout(options.TotalTimeout);

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

                pipeline.AddTimeout(options.AttemptTimeout);
            });

        services.AddHttpClient(BankHealthCheck.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(3));

        // Degraded, not Unhealthy: if the bank is down, taking every gateway instance out of the
        // load balancer would turn "payment rejected" into "gateway unreachable" for merchants.
        services
            .AddHealthChecks()
            .AddCheck<BankHealthCheck>("bank", failureStatus: HealthStatus.Degraded, tags: [ReadinessTag]);
    }
}
