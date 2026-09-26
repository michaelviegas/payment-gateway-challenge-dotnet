using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Observability;

namespace PaymentGateway.Api.Tests.Observability;

public sealed class CorrelationIdPropagationHandlerTests
{
    [Fact]
    public async Task AddsCurrentCorrelationIdToOutgoingRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlation-123";

        HttpRequestMessage? sent = null;
        await new CorrelationIdMiddleware(async httpContext => sent = await SendAsync(httpContext), NullLogger<CorrelationIdMiddleware>.Instance)
            .InvokeAsync(context);

        Assert.Equal("correlation-123", Assert.Single(sent!.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
    }

    [Fact]
    public async Task SendsNoHeaderOutsideARequest()
    {
        var sent = await SendAsync(httpContext: null);

        Assert.False(sent.Headers.Contains(CorrelationIdMiddleware.HeaderName));
    }

    private static async Task<HttpRequestMessage> SendAsync(HttpContext? httpContext)
    {
        var inner = new RecordingHandler();
        var handler = new CorrelationIdPropagationHandler(new HttpContextAccessor { HttpContext = httpContext }) { InnerHandler = inner };

        using var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://bank/payments"), CancellationToken.None);

        return inner.Request!;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
