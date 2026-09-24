namespace PaymentGateway.Application.Abstractions.Messaging;

public interface IIdempotentCommand : IBaseCommand
{
    string IdempotencyKey { get; }
}
