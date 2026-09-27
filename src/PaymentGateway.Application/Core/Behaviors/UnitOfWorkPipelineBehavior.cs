using Mediator;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<UnitOfWorkPipelineBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : Abstractions.Messaging.IBaseCommand
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var commandResponse = await next(request, cancellationToken);

        if (commandResponse.IsSuccess)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Saved changes for {RequestName}", typeof(TRequest).Name);
        }

        return commandResponse;
    }
}
