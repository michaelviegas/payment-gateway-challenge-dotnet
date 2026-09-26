using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Abstractions.Bank;

public sealed record BankAuthorizationResult
{
    public PaymentStatus PaymentStatus { get; }
    public string? AuthorizationCode { get; }

    private BankAuthorizationResult(
        PaymentStatus paymentStatus,
        string? authorizationCode = default)
    {
        PaymentStatus = paymentStatus;
        AuthorizationCode = authorizationCode;
    }

    public bool PaymentRejected => PaymentStatus == PaymentStatus.Rejected;

    public static BankAuthorizationResult Authorized(string authorizationCode) => new(PaymentStatus.Authorized, authorizationCode);
    public static BankAuthorizationResult Declined => new(PaymentStatus.Declined);
    public static BankAuthorizationResult Rejected => new(PaymentStatus.Rejected);
}
