namespace PaymentGateway.Application.Core.Primitives;

public static class ApplicationErrors
{
    public static Error PaymentRejected => Error.From(new ErrorRecord("Payment.Rejected", "No payment could be created as invalid information was supplied to the payment gateway and therefore it has rejected the request without calling the acquiring bank."));

    public static Error PaymentNotFound => Error.From(new ErrorRecord("Payment.NotFound", "The requested payment does not exist."));

    public static Error BankUnavailable => Error.From(new ErrorRecord("Payment.Rejected", "The acquiring bank is unavailable, so the payment was rejected."));
}
