using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Api.Tests.Fixtures;

public sealed class FakeBankClient : IBankClient
{
    private readonly ConcurrentQueue<BankCall> _calls = new();

    public BankAuthorizationResult Result { get; set; } = BankAuthorizationResult.Authorized("auth-code");

    public Exception? Exception { get; set; }

    public TaskCompletionSource? Gate { get; set; }

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyCollection<BankCall> Calls => _calls;

    public async Task<BankAuthorizationResult> AuthorizeAsync(CardDetails card, Money money, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new BankCall(card, money));
        Entered.TrySetResult();

        if (Gate is not null)
        {
            await Gate.Task;
        }

        return Exception is null ? Result : throw Exception;
    }

    public sealed record BankCall(CardDetails Card, Money Money);
}
