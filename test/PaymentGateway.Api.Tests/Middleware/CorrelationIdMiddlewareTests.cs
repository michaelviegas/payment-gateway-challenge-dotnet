using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task EchoesProvidedCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlation-123";

        await InvokeAsync(context);

        Assert.Equal("correlation-123", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeneratesCorrelationIdWhenNoneProvided(string? provided)
    {
        var context = new DefaultHttpContext();
        if (provided is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;
        }

        await InvokeAsync(context);

        Assert.True(Guid.TryParse(context.Response.Headers[CorrelationIdMiddleware.HeaderName], out _));
    }

    public static TheoryData<string> UntrustedIds => new()
    {
        new string('a', CorrelationIdMiddleware.MaxLength + 1),
        "has spaces",
        "line\nbreak",
        "first,second",
        "<script>"
    };

    [Theory]
    [MemberData(nameof(UntrustedIds))]
    public async Task ReplacesUntrustedCorrelationId(string provided)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;

        await InvokeAsync(context);

        Assert.True(Guid.TryParse(context.Response.Headers[CorrelationIdMiddleware.HeaderName], out _));
    }

    [Fact]
    public async Task AcceptsCorrelationIdAtMaximumLength()
    {
        var provided = new string('a', CorrelationIdMiddleware.MaxLength);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;

        await InvokeAsync(context);

        Assert.Equal(provided, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task ExposesCorrelationIdToLaterMiddleware()
    {
        var context = new DefaultHttpContext();
        string? seen = null;

        await InvokeAsync(context, next =>
        {
            seen = CorrelationIdMiddleware.Get(next);
            return Task.CompletedTask;
        });

        Assert.Equal(context.Response.Headers[CorrelationIdMiddleware.HeaderName], seen);
    }

    [Fact]
    public async Task CallsNextMiddleware()
    {
        var called = false;

        await InvokeAsync(new DefaultHttpContext(), _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.True(called);
    }

    private static Task InvokeAsync(HttpContext context, RequestDelegate? next = null) =>
        new CorrelationIdMiddleware(next ?? (_ => Task.CompletedTask), NullLogger<CorrelationIdMiddleware>.Instance)
            .InvokeAsync(context);
}
