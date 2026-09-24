using System.Text.Json.Serialization;

using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Infrastructure.Bank.DTOs;

public sealed record BankAuthorizationRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; } = string.Empty;

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; init; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string Cvv { get; init; } = string.Empty;

    private BankAuthorizationRequest() { }

    public static BankAuthorizationRequest Create(CardDetails card, Money money)
    {
        return new()
        {
            CardNumber = card.Value.CardNumber,
            ExpiryDate = $"{card.Value.ExpiryMonth:D2}/{card.Value.ExpiryYear:D4}",
            Currency = money.Value.Currency,
            Amount = money.Value.Amount,
            Cvv = card.Value.Cvv
        };
    }
}
