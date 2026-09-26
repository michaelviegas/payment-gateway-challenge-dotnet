using System.Collections.Concurrent;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.IntegrationTests.Fixtures;

public sealed class FakeBankClient : IBankClient
{
    private readonly ConcurrentQueue<BankCall> _calls = new();

    public BankAuthorizationResult Result { get; set; } = BankAuthorizationResult.Authorized("auth-code");

    public Exception? Exception { get; set; }

    public IReadOnlyCollection<BankCall> Calls => _calls;

    public Task<BankAuthorizationResult> AuthorizeAsync(CardDetails card, Money money, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new BankCall(card, money));

        return Exception is null ? Task.FromResult(Result) : throw Exception;
    }

    public sealed record BankCall(CardDetails Card, Money Money);
}
