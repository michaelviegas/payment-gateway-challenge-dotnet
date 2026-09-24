using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Domain;
using PaymentGateway.Domain.DomainEvents;

namespace PaymentGateway.Infrastructure.Data;

internal sealed class InMemoryDbContext(InMemoryPaymentStore store) : IAppDbContext, IUnitOfWork
{
    private readonly Dictionary<PaymentId, Payment> _added = [];
    private readonly Dictionary<PaymentId, Payment> _modified = [];
    private readonly Dictionary<PaymentId, Payment> _removed = [];

    public IQueryable<Payment> Payments => store.Snapshot().AsQueryable();

    public void Add(Payment payment)
    {
        if (_added.TryGetValue(payment.Id, out var tracked))
        {
            if (ReferenceEquals(tracked, payment)) return;

            throw new InvalidOperationException(
                $"Another instance of payment {payment.Id} is already being tracked.");
        }

        _added.Add(payment.Id, payment);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_added.Count == 0 && _modified.Count == 0 && _removed.Count == 0) return;

        var entities = _added.Values.Concat(_modified.Values).ToList<IHasDomainEvents>();
        var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToList();

        store.Commit(_added.Values, _modified.Values, _removed.Values);
        _added.Clear();
        _modified.Clear();
        _removed.Clear();

        entities.ForEach(entity => entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            // Just for demo purposes. Placeholder to publish the domain events.
        }
    }
}
