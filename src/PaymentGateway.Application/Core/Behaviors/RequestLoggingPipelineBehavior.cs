using Mediator;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting request {RequestName}", typeof(TRequest).Name);

        var result = await next(request, cancellationToken);

        // Failures here are expected outcomes (validation, not found, bank rejection), not faults.
        // Exceptions are logged at Error by the API's exception middleware.
        if (result.IsFailure)
        {
            // Codes only: they are stable and searchable. Validation messages can quote the
            // submitted value, which may be card data.
            logger.LogWarning(
                "Request {RequestName} failed with {ErrorCodes}",
                typeof(TRequest).Name,
                string.Join(", ", result.Errors.Select(error => error.Value.Code)));
        }

        logger.LogInformation("Completed request {RequestName}", typeof(TRequest).Name);

        return result;
    }
}
