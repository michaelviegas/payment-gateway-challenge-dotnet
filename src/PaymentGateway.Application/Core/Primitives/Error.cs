using Vogen;

namespace PaymentGateway.Application.Core.Primitives;

public record ErrorRecord(string Code, string Message);

[ValueObject<ErrorRecord>]
public readonly partial record struct Error
{
    internal static Error None => Error.From(new(string.Empty, string.Empty));
    internal static Error NullValue => Error.From(new("Error.NullValue", "Null value was provided"));
}
