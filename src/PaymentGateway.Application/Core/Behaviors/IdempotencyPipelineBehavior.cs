using Mediator;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Abstractions.Idempotency;
using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class IdempotencyPipelineBehavior<TRequest, TResponse>(
    IIdempotencyStore idempotencyStore,
    ILogger<IdempotencyPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Failure(ApplicationErrors.IdempotencyKeyMissing);
        }

        var key = $"{typeof(TRequest).Name}:{request.IdempotencyKey}";

        var existing = await idempotencyStore.TryClaimAsync(key, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Idempotency key already claimed for {RequestName}", typeof(TRequest).Name);

            return existing.Response as TResponse ?? Failure(ApplicationErrors.IdempotentRequestInProgress);
        }

        var response = await next(request, cancellationToken);

        if (response.IsSuccess)
        {
            await idempotencyStore.CompleteAsync(key, response, CancellationToken.None);
        }
        else
        {
            await idempotencyStore.ReleaseAsync(key, CancellationToken.None);
        }

        return response;
    }

    private static TResponse Failure(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        var failure = typeof(Result).GetMethods()
            .Single(method => method.Name == nameof(Result.Failure)
                && method.IsGenericMethod
                && method.GetParameters()[0].ParameterType == typeof(Error))
            .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);

        return (TResponse)failure.Invoke(null, [error])!;
    }
}
