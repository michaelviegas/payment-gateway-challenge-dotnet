using Mediator;

using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Abstractions.Messaging;

public interface IBaseCommand : IMessage;

public interface ICommand : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
