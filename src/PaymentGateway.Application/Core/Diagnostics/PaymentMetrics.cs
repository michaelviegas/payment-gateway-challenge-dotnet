using System.Diagnostics.Metrics;

using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Core.Diagnostics;

/// <summary>
/// Business metrics for payments sent to the acquiring bank. HTTP, HttpClient and Polly
/// metrics come from their own instrumentation; this adds what only the handler knows.
/// </summary>
public sealed class PaymentMetrics
{
    public const string MeterName = "PaymentGateway.Payments";

    internal const string RejectedOutcome = "Rejected";

    private readonly Counter<long> _processed;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _processed = meter.CreateCounter<long>(
            "payments.processed",
            unit: "{payment}",
            description: "Payments sent to the acquiring bank, by outcome and currency.");
    }

    internal void PaymentProcessed(PaymentStatus status, string currency) => Record(status.ToString(), currency);

    internal void PaymentRejected(string currency) => Record(RejectedOutcome, currency);

    private void Record(string outcome, string currency) =>
        _processed.Add(1, new KeyValuePair<string, object?>("outcome", outcome), new KeyValuePair<string, object?>("currency", currency));
}
