namespace PaymentGateway.Application.Core.Primitives;

public class Result
{
    protected internal Result(bool isSuccess, IEnumerable<Error> errors)
    {
        if (isSuccess == errors.Any())
        {
            throw new ArgumentException("Invalid error", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors.ToList();
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(Error error) => new(false, [error]);

    public static Result Failure(IEnumerable<Error> errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(value, true, []);

    public static Result<T> Failure<T>(Error error) => new(default!, false, [error]);

    public static Result<T> Failure<T>(IEnumerable<Error> errors) => new(default!, false, errors);

    public static Result<T> Create<T>(T? value) =>
        value is not null ? Success(value) : Failure<T>(Error.NullValue);
}
