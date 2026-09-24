using PaymentGateway.Domain;

namespace PaymentGateway.Application.Abstractions.Data;

public interface IPaymentRepository
{
    void Add(Payment entity);
    Payment? Find(PaymentId id);
}
