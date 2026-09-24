using System.Net;

using PaymentGateway.Api.Tests.Fixtures;
using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Commands.PostPayment;

namespace PaymentGateway.Api.Tests;

public sealed class IdempotencyTests : PaymentGatewayTestBase
{
    private const string Key = "key-1";

    [Fact]
    public async Task RetryReplaysOriginalPaymentWithoutCallingBankAgain()
    {
        var first = await (await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key)).ReadAsAsync<PostPaymentResponse>();

        var retry = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);
        var replayed = await retry.ReadAsAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(first, replayed);
        Assert.Single(Bank.Calls);
    }

    [Fact]
    public async Task DifferentKeysCreateSeparatePayments()
    {
        var first = await (await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), "key-1")).ReadAsAsync<PostPaymentResponse>();
        var second = await (await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), "key-2")).ReadAsAsync<PostPaymentResponse>();

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, Bank.Calls.Count);
    }

    [Fact]
    public async Task RejectedPaymentReleasesKeySoItCanBeRetried()
    {
        Bank.Result = BankAuthorizationResult.Rejected;
        var rejected = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);

        Bank.Result = BankAuthorizationResult.Authorized("auth-code");
        var retry = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(2, Bank.Calls.Count);
    }

    [Fact]
    public async Task InvalidRequestDoesNotClaimKey()
    {
        var invalid = PaymentsApi.ValidRequest();
        invalid.Amount = 0;

        var rejected = await Client.PostPaymentAsync(invalid, Key);
        var retry = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task FailureAfterBankCallKeepsKeyClaimed()
    {
        Bank.Exception = new InvalidOperationException("Simulated failure after the bank call.");
        var failed = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);

        Bank.Exception = null;
        var retry = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Single(Bank.Calls);
    }

    [Fact]
    public async Task ConcurrentRequestsWithSameKeyCallBankOnce()
    {
        Bank.Gate = new TaskCompletionSource();

        var first = Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);
        await Bank.Entered.Task;

        var second = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), Key);
        Bank.Gate.SetResult();

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
        Assert.Single(Bank.Calls);
    }
}
