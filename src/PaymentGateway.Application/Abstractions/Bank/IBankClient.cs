using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Abstractions.Bank;

public interface IBankClient
{
    Task<BankAuthorizationResult> AuthorizeAsync(CardDetails card, Money money, CancellationToken cancellationToken);
}
