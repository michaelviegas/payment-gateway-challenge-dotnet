using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Tests.Fixtures;
using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests;

public sealed class PostPaymentTests : PaymentGatewayTestBase
{
    [Fact]
    public async Task AuthorizedPaymentReturnsPaymentDetails()
    {
        var request = PaymentsApi.ValidRequest();

        var response = await Client.PostPaymentAsync(request);
        var payment = await response.ReadAsAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(8877, payment.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, payment.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, payment.ExpiryYear);
        Assert.Equal(request.Currency, payment.Currency);
        Assert.Equal(request.Amount, payment.Amount);
    }

    [Fact]
    public async Task DeclinedPaymentIsReturnedWithDeclinedStatus()
    {
        Bank.Result = BankAuthorizationResult.Declined;

        var response = await Client.PostPaymentAsync(PaymentsApi.ValidRequest());
        var payment = await response.ReadAsAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Declined, payment.Status);
    }

    [Fact]
    public async Task ResponseExposesOnlyMaskedCardDetailsAndStatusAsText()
    {
        var response = await Client.PostPaymentAsync(PaymentsApi.ValidRequest());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var properties = json.RootElement.EnumerateObject().Select(property => property.Name);

        Assert.Equal(
            ["id", "status", "cardNumberLastFour", "expiryMonth", "expiryYear", "currency", "amount"],
            properties);
        Assert.Equal("Authorized", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ForwardsCardDetailsAndAmountToBank()
    {
        var request = PaymentsApi.ValidRequest();

        await Client.PostPaymentAsync(request);

        var call = Assert.Single(Bank.Calls);
        Assert.Equal(request.CardNumber, call.Card.Value.CardNumber);
        Assert.Equal(request.ExpiryMonth, call.Card.Value.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, call.Card.Value.ExpiryYear);
        Assert.Equal(request.Cvv, call.Card.Value.Cvv);
        Assert.Equal(request.Currency, call.Money.Value.Currency);
        Assert.Equal(request.Amount, call.Money.Value.Amount);
    }

    [Fact]
    public async Task PaymentRejectedByBankReturnsBadRequest()
    {
        Bank.Result = BankAuthorizationResult.Rejected;

        var response = await Client.PostPaymentAsync(PaymentsApi.ValidRequest());
        var problem = await response.ReadAsAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Payment.Rejected", problem.Title);
    }

    [Fact]
    public async Task UnhandledBankFailureReturnsInternalServerError()
    {
        Bank.Exception = new InvalidOperationException("Simulated failure.");

        var response = await Client.PostPaymentAsync(PaymentsApi.ValidRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKeyIsRejectedWithoutCallingBank()
    {
        var response = await Client.PostPaymentAsync(PaymentsApi.ValidRequest(), idempotencyKey: null);
        var problem = await response.ReadAsAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Idempotency.KeyMissing", problem.Title);
        Assert.Empty(Bank.Calls);
    }
}
