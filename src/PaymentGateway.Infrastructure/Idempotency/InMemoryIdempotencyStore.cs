using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions.Idempotency;

namespace PaymentGateway.Infrastructure.Idempotency;

internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly IdempotencyRecord InFlight = new(Response: null);

    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new(StringComparer.Ordinal);

    public Task<IdempotencyRecord?> TryClaimAsync(string key, CancellationToken cancellationToken)
    {
        var claimed = _records.TryAdd(key, InFlight);

        return Task.FromResult(claimed ? null : _records.GetValueOrDefault(key, InFlight));
    }

    public Task CompleteAsync(string key, object response, CancellationToken cancellationToken)
    {
        _records[key] = new IdempotencyRecord(response);

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string key, CancellationToken cancellationToken)
    {
        _records.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}
