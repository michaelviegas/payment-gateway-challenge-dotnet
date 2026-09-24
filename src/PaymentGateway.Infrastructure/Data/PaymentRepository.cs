using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Domain;

namespace PaymentGateway.Infrastructure.Data;

internal sealed class PaymentRepository(IAppDbContext appDbContext) : IPaymentRepository
{
    public void Add(Payment entity)
    {
        appDbContext.Add(entity);
    }

    public Payment? Find(PaymentId id)
    {
        return appDbContext.Payments.FirstOrDefault(payment => payment.Id == id);
    }
}
