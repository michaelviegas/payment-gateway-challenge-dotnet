using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Infrastructure.Bank;

internal sealed class BankOptions
{
    public const string SectionName = "Bank";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Limit for a single call to the bank.</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Limit for the whole call, including connection retries.</summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
