using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Domain;

namespace PaymentGateway.Application.Queries.GetPayment;

internal sealed class GetPaymentQueryHandler(IPaymentRepository payments)
    : IQueryHandler<GetPaymentQuery, GetPaymentResponse>
{
    public ValueTask<Result<GetPaymentResponse>> Handle(GetPaymentQuery query, CancellationToken cancellationToken)
    {
        var payment = payments.Find(PaymentId.From(query.Id));

        if (payment is null)
        {
            return ValueTask.FromResult(Result.Failure<GetPaymentResponse>(ApplicationErrors.PaymentNotFound));
        }

        return ValueTask.FromResult(Result.Success(new GetPaymentResponse
        {
            Id = payment.Id.Value,
            Status = payment.Status,
            CardNumberLastFour = payment.Card.Value.CardNumberLastFour,
            ExpiryMonth = payment.Card.Value.ExpiryMonth,
            ExpiryYear = payment.Card.Value.ExpiryYear,
            Currency = payment.Money.Value.Currency,
            Amount = payment.Money.Value.Amount
        }));
    }
}
