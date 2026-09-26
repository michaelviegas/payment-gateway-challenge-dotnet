using FluentValidation;
using Mediator;
using PaymentGateway.Application.Core.Primitives;
using System.Reflection;

namespace PaymentGateway.Application.Core.Behaviors;

internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateAsync(request);

        if (validationErrors.Count > 0)
        {
            return (TResponse)FailureMethod.Invoke(null, new[] { validationErrors })!;
        }

        return await next(request, cancellationToken);
    }

    private async Task<IReadOnlyCollection<Error>> ValidateAsync(TRequest request)
    {
        if (!validators.Any())
        {
            return [];
        }

        // Each validator gets its own context: a shared one accumulates every validator's failures into each result.
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(new ValidationContext<TRequest>(request))));

        var validationFailures = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Select(validationFailure => Error.From(new ErrorRecord(
                validationFailure.PropertyName,
                validationFailure.ErrorMessage)))
            .ToList();

        return validationFailures;
    }

    private MethodInfo FailureMethod
    {
        get
        {
            var isGeneric = typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>);
            var failureMethod = typeof(Result).GetMethods()
                .First(m => m.Name == "Failure"
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(IEnumerable<Error>)
                    && (!isGeneric || m.IsGenericMethod && m.GetGenericArguments().Length == 1));
            return isGeneric ? failureMethod.MakeGenericMethod(typeof(TResponse).GetGenericArguments().First()) : failureMethod;
        }
    }
}
