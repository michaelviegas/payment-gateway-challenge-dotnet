using Mediator;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting Request {@RequestName}, {@DateTimeUtc}",
            typeof(TRequest).Name,
            timeProvider.GetUtcNow());

        var result = await next(request, cancellationToken);

        // Failures here are expected outcomes (validation, not found, bank rejection), not faults.
        // Exceptions are logged at Error by the API's exception middleware.
        if (result.IsFailure)
        {
            logger.LogWarning("Request Failure {@RequestName}, {@Errors}, {@DateTimeUtc}",
                typeof(TRequest).Name,
                result.Errors,
                timeProvider.GetUtcNow());
        }

        logger.LogInformation(
            "Completed Request {@RequestName}, {@DateTimeUtc}",
            typeof(TRequest).Name,
            timeProvider.GetUtcNow());

        return result;
    }
}
