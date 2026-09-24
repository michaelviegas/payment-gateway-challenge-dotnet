using System.Text.Json.Serialization;

using PaymentGateway.Application.Commands.PostPayment;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Cvv { get; set; } = string.Empty;

    internal PostPaymentCommand ToCommand(string? idempotencyKey) => new(idempotencyKey ?? string.Empty, CardNumber, ExpiryMonth, ExpiryYear, Currency, Amount, Cvv);
}