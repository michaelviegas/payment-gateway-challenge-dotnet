using PaymentGateway.Application.Abstractions.Messaging;

namespace PaymentGateway.Application.Queries.GetPayment;

public sealed record GetPaymentQuery(Guid Id) : IQuery<GetPaymentResponse>;