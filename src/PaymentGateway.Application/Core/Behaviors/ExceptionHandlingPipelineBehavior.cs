using Mediator;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class ExceptionHandlingPipelineBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(request, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing the request {@RequestName}", typeof(TRequest).Name);

            throw;
        }
    }

}
