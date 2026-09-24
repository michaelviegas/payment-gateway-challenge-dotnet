using System.Diagnostics.CodeAnalysis;

namespace PaymentGateway.Application.Core.Primitives;

public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T value, bool isSuccess, IEnumerable<Error> errors) : base(isSuccess, errors)
    {
        _value = value;
    }

    [NotNull]
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed");

    public static implicit operator Result<T>(T? value) => Create(value);
}
