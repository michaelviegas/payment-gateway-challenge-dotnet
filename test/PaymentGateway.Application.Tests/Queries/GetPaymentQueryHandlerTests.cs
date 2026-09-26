using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Application.Queries.GetPayment;
using PaymentGateway.Application.Tests.Fakes;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Tests.Queries;

public sealed class GetPaymentQueryHandlerTests
{
    private readonly FakePaymentRepository _repository = new();

    [Fact]
    public async Task ReturnsStoredPayment()
    {
        var payment = Payment.Create(
            PaymentStatus.Declined,
            CardInfo.From(new CardInfoRecord("8877", 4, 2030)),
            Money.From(new MoneyRecord("USD", 250)));
        _repository.Add(payment);

        var result = await Handle(payment.Id.Value);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new GetPaymentResponse
            {
                Id = payment.Id.Value,
                Status = PaymentStatus.Declined,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = 250
            },
            result.Value);
    }

    [Fact]
    public async Task UnknownPaymentFailsWithNotFound()
    {
        var result = await Handle(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal([ApplicationErrors.PaymentNotFound], result.Errors);
    }

    private ValueTask<Result<GetPaymentResponse>> Handle(Guid id) =>
        new GetPaymentQueryHandler(_repository).Handle(new GetPaymentQuery(id), CancellationToken.None);
}
