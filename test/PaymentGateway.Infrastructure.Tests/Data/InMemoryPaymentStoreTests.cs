using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Infrastructure.Data;

namespace PaymentGateway.Infrastructure.Tests.Data;

public sealed class InMemoryPaymentStoreTests
{
    private readonly InMemoryPaymentStore _store = new();

    [Fact]
    public void StartsEmpty()
    {
        Assert.Empty(_store.Snapshot());
    }

    [Fact]
    public void CommitAddsPayments()
    {
        var payment = Payments.New();

        _store.Commit([payment], [], []);

        Assert.Equal(payment.Id, Assert.Single(_store.Snapshot()).Id);
    }

    [Fact]
    public void SnapshotReturnsCopies()
    {
        var payment = Payments.New();
        _store.Commit([payment], [], []);

        var stored = Assert.Single(_store.Snapshot());

        Assert.NotSame(payment, stored);
        Assert.Equal(payment.Status, stored.Status);
    }

    [Fact]
    public void CommitReplacesModifiedPayments()
    {
        var payment = Payments.New();
        _store.Commit([payment], [], []);

        _store.Commit([], [payment with { Status = PaymentStatus.Declined }], []);

        Assert.Equal(PaymentStatus.Declined, Assert.Single(_store.Snapshot()).Status);
    }

    [Fact]
    public void CommitRemovesPayments()
    {
        var payment = Payments.New();
        _store.Commit([payment], [], []);

        _store.Commit([], [], [payment]);

        Assert.Empty(_store.Snapshot());
    }

    [Fact]
    public void AddingExistingIdThrowsAndChangesNothing()
    {
        var existing = Payments.New();
        _store.Commit([existing], [], []);
        var other = Payments.New();

        Assert.Throws<InvalidOperationException>(() => _store.Commit([other, existing], [], []));

        Assert.Equal(existing.Id, Assert.Single(_store.Snapshot()).Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChangingMissingPaymentThrowsAndChangesNothing(bool modify)
    {
        var missing = Payments.New();
        var added = Payments.New();
        IReadOnlyCollection<Payment> changed = [missing];

        Assert.Throws<InvalidOperationException>(() =>
            _store.Commit([added], modify ? changed : [], modify ? [] : changed));

        Assert.Empty(_store.Snapshot());
    }
}
