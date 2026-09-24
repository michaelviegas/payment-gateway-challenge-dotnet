using PaymentGateway.Domain;

namespace PaymentGateway.Infrastructure.Data;

/// <summary>
/// Stands in for the database: holds committed payments only, shared across requests.
/// Payments are copied in and out, so uncommitted changes to a tracked instance are never visible here.
/// </summary>
internal sealed class InMemoryPaymentStore
{
    private readonly object _gate = new();
    private readonly Dictionary<PaymentId, Payment> _payments = [];

    public IReadOnlyList<Payment> Snapshot()
    {
        lock (_gate)
        {
            return [.. _payments.Values.Select(Copy)];
        }
    }

    /// <summary>
    /// Applies all changes or none, like a single database transaction.
    /// </summary>
    public void Commit(
        IReadOnlyCollection<Payment> added,
        IReadOnlyCollection<Payment> modified,
        IReadOnlyCollection<Payment> removed)
    {
        lock (_gate)
        {
            var missing = modified.Concat(removed).FirstOrDefault(payment => !_payments.ContainsKey(payment.Id));
            if (missing is not null)
            {
                throw new InvalidOperationException($"Cannot change payment {missing.Id}: it does not exist.");
            }

            var conflict = added.FirstOrDefault(payment => _payments.ContainsKey(payment.Id));
            if (conflict is not null)
            {
                throw new InvalidOperationException(
                    $"Cannot insert payment {conflict.Id}: a payment with the same key already exists.");
            }

            foreach (var payment in removed)
            {
                _payments.Remove(payment.Id);
            }

            foreach (var payment in added)
            {
                _payments.Add(payment.Id, Copy(payment));
            }

            foreach (var payment in modified)
            {
                _payments[payment.Id] = Copy(payment);
            }
        }
    }

    private static Payment Copy(Payment payment) => payment with { };
}
