using System.Net;

using PaymentGateway.IntegrationTests.Fixtures;
using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Queries.GetPayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.IntegrationTests;

public sealed class GetPaymentTests : PaymentGatewayTestBase
{
    [Fact]
    public async Task ReturnsAuthorizedPayment()
    {
        var posted = await PostPaymentAsync();

        var response = await Client.GetPaymentAsync(posted.Id);
        var payment = await response.ReadAsAsync<GetPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equivalent(posted, payment, strict: true);
    }

    [Fact]
    public async Task ReturnsDeclinedPayment()
    {
        Bank.Result = BankAuthorizationResult.Declined;
        var posted = await PostPaymentAsync();

        var payment = await (await Client.GetPaymentAsync(posted.Id)).ReadAsAsync<GetPaymentResponse>();

        Assert.Equal(PaymentStatus.Declined, payment.Status);
    }

    [Fact]
    public async Task ReturnsEachPaymentById()
    {
        var first = await PostPaymentAsync();
        var second = await PostPaymentAsync();

        var fetchedFirst = await (await Client.GetPaymentAsync(first.Id)).ReadAsAsync<GetPaymentResponse>();
        var fetchedSecond = await (await Client.GetPaymentAsync(second.Id)).ReadAsAsync<GetPaymentResponse>();

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Id, fetchedFirst.Id);
        Assert.Equal(second.Id, fetchedSecond.Id);
    }

    [Fact]
    public async Task ReturnsNotFoundForUnknownPayment()
    {
        var response = await Client.GetPaymentAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsNotFoundForMalformedId()
    {
        var response = await Client.GetAsync("/api/Payments/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PostPaymentResponse> PostPaymentAsync()
    {
        var response = await Client.PostPaymentAsync(ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.ReadAsAsync<PostPaymentResponse>();
    }
}
