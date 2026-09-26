using System.Diagnostics;

namespace PaymentGateway.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    internal const int MaxLength = 64;

    private static readonly object ItemKey = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>The correlation id of the current request, or null outside the middleware.</summary>
    public static string? Get(HttpContext context) => context.Items[ItemKey] as string;

    public async Task InvokeAsync(HttpContext context)
    {
        // The id comes from the caller and ends up in logs, traces and the bank request,
        // so anything long or unusual is replaced rather than trusted.
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var provided) && IsValid(provided.ToString())
            ? provided.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceIdentifier"] = context.TraceIdentifier
        }))
        {
            await _next(context);
        }
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= MaxLength
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');
}
