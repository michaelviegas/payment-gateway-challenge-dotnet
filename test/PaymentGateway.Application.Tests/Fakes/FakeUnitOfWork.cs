using PaymentGateway.Application.Abstractions.Data;

namespace PaymentGateway.Application.Tests.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;

        return Task.CompletedTask;
    }
}
