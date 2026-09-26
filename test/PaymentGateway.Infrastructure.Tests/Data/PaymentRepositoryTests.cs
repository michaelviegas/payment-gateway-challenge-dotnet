using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Data;

namespace PaymentGateway.Infrastructure.Tests.Data;

public sealed class PaymentRepositoryTests
{
    private readonly InMemoryPaymentStore _store = new();

    [Fact]
    public async Task FindsSavedPayment()
    {
        var context = new InMemoryDbContext(_store);
        var payment = Payments.New();
        new PaymentRepository(context).Add(payment);
        await context.SaveChangesAsync(CancellationToken.None);

        var found = new PaymentRepository(new InMemoryDbContext(_store)).Find(payment.Id);

        Assert.NotNull(found);
        Assert.Equal(payment.Id, found.Id);
        Assert.Equal(payment.Card, found.Card);
        Assert.Equal(payment.Money, found.Money);
    }

    [Fact]
    public void FindReturnsNullForUnknownPayment()
    {
        var repository = new PaymentRepository(new InMemoryDbContext(_store));

        Assert.Null(repository.Find(PaymentId.From(Guid.NewGuid())));
    }
}
