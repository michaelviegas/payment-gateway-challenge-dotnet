using PaymentGateway.Domain.DomainEvents;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain
{
    public sealed record Payment : IHasDomainEvents
    {
        public PaymentId Id { get; init; }
        public PaymentStatus Status { get; init; }
        public CardInfo Card { get; init; }
        public Money Money { get; init; }

        #region Domain Events
        private readonly List<IDomainEvent> _domainEvents = [];

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void ClearDomainEvents() => _domainEvents.Clear();

        private void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        #endregion

        private Payment() { }
        public static Payment Create(
            PaymentStatus status,
            CardInfo card,
            Money money) 
        {
            var payment = new Payment() 
            { 
                Id = PaymentId.New(),
                Status = status,
                Card = card,
                Money = money
            };

            // Just for demo purposes. Use Case: After a payment is created, a notification is sent.
            payment.RaiseDomainEvent(new PaymentCreatedDomainEvent(payment.Id));

            return payment;       
        }
    }
}
