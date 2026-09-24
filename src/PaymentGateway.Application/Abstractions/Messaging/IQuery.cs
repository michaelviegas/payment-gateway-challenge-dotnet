using Mediator;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
