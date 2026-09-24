namespace PaymentGateway.Application.Core.Primitives;

public static class ApplicationErrors
{
    public static Error PaymentRejected = Error.From(new("Payment.Rejected", "No payment could be created as invalid information was supplied to the payment gateway and therefore it has rejected the request without calling the acquiring bank."));

    public static Error PaymentNotFound => Error.From(new ErrorRecord("Payment.NotFound", "The requested payment does not exist."));

    public static Error IdempotencyKeyMissing => Error.From(new ErrorRecord("Idempotency.KeyMissing", "An Idempotency-Key header is required."));

    public static Error IdempotentRequestInProgress => Error.From(new ErrorRecord("Idempotency.InProgress", "A request with this idempotency key is still being processed or awaiting reconciliation. Do not retry it with a new key."));
}
