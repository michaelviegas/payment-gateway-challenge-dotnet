using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Application.Commands.PostPayment;

namespace PaymentGateway.Api.Tests.Models;

public sealed class PostPaymentRequestTests
{
    [Fact]
    public void ToCommandCopiesEveryField()
    {
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        Assert.Equal(new PostPaymentCommand("2222405343248877", 4, 2030, "GBP", 100, "123"), request.ToCommand());
    }
}
