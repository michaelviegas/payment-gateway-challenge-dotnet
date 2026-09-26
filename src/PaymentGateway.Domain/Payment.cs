using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain
{
    public sealed record Payment
    {
        public PaymentId Id { get; init; }
        public PaymentStatus Status { get; init; }
        public CardInfo Card { get; init; }
        public Money Money { get; init; }

        private Payment() { }
        public static Payment Create(
            PaymentStatus status,
            CardInfo card,
            Money money)
        {
            return new Payment()
            {
                Id = PaymentId.New(),
                Status = status,
                Card = card,
                Money = money
            };
        }
    }
}
