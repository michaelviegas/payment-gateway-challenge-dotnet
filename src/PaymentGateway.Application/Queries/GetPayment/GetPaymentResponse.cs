using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Queries.GetPayment;

public sealed record GetPaymentResponse
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; init; }
    public int CardNumberLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public int Amount { get; init; }
}