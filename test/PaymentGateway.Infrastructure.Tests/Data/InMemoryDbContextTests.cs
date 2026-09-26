using PaymentGateway.Infrastructure.Data;

namespace PaymentGateway.Infrastructure.Tests.Data;

public sealed class InMemoryDbContextTests
{
    private readonly InMemoryPaymentStore _store = new();

    [Fact]
    public void AddedPaymentIsNotVisibleUntilSaved()
    {
        var context = new InMemoryDbContext(_store);

        context.Add(Payments.New());

        Assert.Empty(context.Payments);
    }

    [Fact]
    public async Task SaveChangesCommitsAddedPayments()
    {
        var context = new InMemoryDbContext(_store);
        var payment = Payments.New();
        context.Add(payment);

        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(payment.Id, Assert.Single(new InMemoryDbContext(_store).Payments).Id);
    }

    [Fact]
    public async Task SaveChangesTwiceDoesNotReinsert()
    {
        var context = new InMemoryDbContext(_store);
        context.Add(Payments.New());

        await context.SaveChangesAsync(CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Single(_store.Snapshot());
    }

    [Fact]
    public async Task AddingSameInstanceTwiceIsIgnored()
    {
        var context = new InMemoryDbContext(_store);
        var payment = Payments.New();

        context.Add(payment);
        context.Add(payment);
        await context.SaveChangesAsync(CancellationToken.None);

        Assert.Single(_store.Snapshot());
    }

    [Fact]
    public void AddingDifferentInstanceWithSameIdThrows()
    {
        var context = new InMemoryDbContext(_store);
        var payment = Payments.New();
        context.Add(payment);

        Assert.Throws<InvalidOperationException>(() => context.Add(payment with { }));
    }

    [Fact]
    public async Task CancelledSaveCommitsNothing()
    {
        var context = new InMemoryDbContext(_store);
        context.Add(Payments.New());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.SaveChangesAsync(new CancellationToken(canceled: true)));

        Assert.Empty(_store.Snapshot());
    }
}
