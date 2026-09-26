namespace PaymentGateway.Domain.Tests;

public sealed class PaymentIdTests
{
    [Fact]
    public void NewCreatesNonEmptyUniqueIds()
    {
        var first = PaymentId.New();
        var second = PaymentId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void IdsWithSameGuidAreEqual()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(PaymentId.From(guid), PaymentId.From(guid));
    }
}
