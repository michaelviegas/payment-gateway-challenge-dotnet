namespace PaymentGateway.Application.Abstractions.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> TryClaimAsync(string key, CancellationToken cancellationToken);

    Task CompleteAsync(string key, object response, CancellationToken cancellationToken);

    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}

public sealed record IdempotencyRecord(object? Response);
