using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Tests;

public sealed class BankAuthorizationResultTests
{
    [Fact]
    public void AuthorizedCarriesAuthorizationCode()
    {
        var result = BankAuthorizationResult.Authorized("auth-code");

        Assert.Equal(PaymentStatus.Authorized, result.PaymentStatus);
        Assert.Equal("auth-code", result.AuthorizationCode);
        Assert.False(result.PaymentRejected);
    }

    [Fact]
    public void DeclinedIsNotRejected()
    {
        Assert.Equal(PaymentStatus.Declined, BankAuthorizationResult.Declined.PaymentStatus);
        Assert.False(BankAuthorizationResult.Declined.PaymentRejected);
    }

    [Fact]
    public void RejectedIsRejected()
    {
        Assert.Equal(PaymentStatus.Rejected, BankAuthorizationResult.Rejected.PaymentStatus);
        Assert.True(BankAuthorizationResult.Rejected.PaymentRejected);
    }
}
