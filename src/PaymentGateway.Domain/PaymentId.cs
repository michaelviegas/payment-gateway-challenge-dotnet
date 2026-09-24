using Vogen;

namespace PaymentGateway.Domain;

[ValueObject<Guid>]
public partial record struct PaymentId
{
    internal static PaymentId New() => From(Guid.NewGuid());
}