using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using PaymentGateway.Infrastructure.Bank;

namespace PaymentGateway.Infrastructure.Tests.Bank;

public sealed class BankHealthCheckTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task AnyHttpResponseIsHealthy(HttpStatusCode status)
    {
        var result = await CheckAsync(StubHttpHandler.Returning(status));

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task UnreachableBankReportsFailureStatus()
    {
        var result = await CheckAsync(StubHttpHandler.Throwing(new HttpRequestException(HttpRequestError.ConnectionError)));

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.IsType<HttpRequestException>(result.Exception);
    }

    [Fact]
    public async Task SlowBankReportsFailureStatusInsteadOfThrowing()
    {
        var result = await CheckAsync(new HangingHandler(), timeout: TimeSpan.FromMilliseconds(50));

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Acquiring bank did not respond in time.", result.Description);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsBankFailure()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CheckAsync(new HangingHandler(), cancellationToken: cancelled.Token));
    }

    private static Task<HealthCheckResult> CheckAsync(
        HttpMessageHandler handler,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var check = new BankHealthCheck(
            new StubHttpClientFactory(handler, timeout ?? TimeSpan.FromSeconds(3)),
            Options.Create(new BankOptions { BaseUrl = "http://bank.test" }));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("bank", check, HealthStatus.Degraded, tags: null)
        };

        return check.CheckHealthAsync(context, cancellationToken);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler, TimeSpan timeout) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = timeout };
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new UnreachableException();
        }
    }
}
