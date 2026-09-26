using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Domain;

namespace PaymentGateway.Application.Tests.Fakes;

internal sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments = [];

    public IReadOnlyList<Payment> Payments => _payments;

    public void Add(Payment entity) => _payments.Add(entity);

    public Payment? Find(PaymentId id) => _payments.FirstOrDefault(payment => payment.Id == id);
}
