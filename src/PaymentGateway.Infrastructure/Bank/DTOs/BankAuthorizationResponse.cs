using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.Bank.DTOs;

public sealed record BankAuthorizationResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; init; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }
}
