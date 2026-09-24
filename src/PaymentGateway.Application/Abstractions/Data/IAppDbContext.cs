using PaymentGateway.Domain;

namespace PaymentGateway.Application.Abstractions.Data;

public interface IAppDbContext
{
    IQueryable<Payment> Payments { get; }

    void Add(Payment payment);
}
