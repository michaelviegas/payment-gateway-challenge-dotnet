namespace PaymentGateway.Domain.DomainEvents
{
    public sealed record PaymentCreatedDomainEvent(PaymentId Id) : IDomainEvent;
}
