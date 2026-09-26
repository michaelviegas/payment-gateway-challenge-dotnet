using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Tests.Fakes;

internal sealed class FakeBankClient : IBankClient
{
    private readonly List<(CardDetails Card, Money Money)> _calls = [];

    public BankAuthorizationResult Result { get; set; } = BankAuthorizationResult.Authorized("auth-code");

    public IReadOnlyList<(CardDetails Card, Money Money)> Calls => _calls;

    public Task<BankAuthorizationResult> AuthorizeAsync(CardDetails card, Money money, CancellationToken cancellationToken)
    {
        _calls.Add((card, money));

        return Task.FromResult(Result);
    }
}
