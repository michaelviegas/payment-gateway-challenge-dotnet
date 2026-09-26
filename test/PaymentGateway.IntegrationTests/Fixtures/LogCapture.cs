using System.Collections.Concurrent;
using System.Text;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PaymentGateway.IntegrationTests.Fixtures;

/// <summary>Serilog sink that keeps every event, rendered as it would be shipped: message, properties and exception.</summary>
public sealed class LogCapture : ILogEventSink
{
    private static readonly RenderedCompactJsonFormatter Formatter = new();

    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyCollection<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    public string Render()
    {
        var output = new StringWriter(new StringBuilder());

        foreach (var logEvent in _events)
        {
            Formatter.Format(logEvent, output);
        }

        return output.ToString();
    }
}
