using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Observability;

/// <summary>
/// Adds the current request's correlation id to outgoing HTTP calls, so a payment can be
/// followed from the merchant's request into the bank's logs.
/// </summary>
/// <remarks>
/// It lives in the API, the composition root, because only the API knows about the incoming
/// request. Infrastructure's HttpClients pick it up without depending on ASP.NET Core.
/// </remarks>
internal sealed class CorrelationIdPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext is { } context ? CorrelationIdMiddleware.Get(context) : null;

        if (correlationId is not null && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
