using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Infrastructure.Bank;

/// <summary>
/// Checks the acquiring bank can be reached. It uses its own HttpClient, outside the payment
/// resilience pipeline, so probes never count towards the circuit breaker.
/// </summary>
internal sealed class BankHealthCheck(IHttpClientFactory httpClientFactory, IOptions<BankOptions> options) : IHealthCheck
{
    public const string HttpClientName = "bank-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            // Any HTTP response means the bank is reachable. The simulator answers unknown routes with 400.
            using var response = await client.GetAsync(options.Value.BaseUrl, cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (HttpRequestException exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Acquiring bank is unreachable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // The probe's own HttpClient timeout, not the caller giving up.
            return new HealthCheckResult(context.Registration.FailureStatus, "Acquiring bank did not respond in time.", exception);
        }
    }
}
