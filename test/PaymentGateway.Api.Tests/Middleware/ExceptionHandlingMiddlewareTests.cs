using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task LeavesSuccessfulResponseUntouched()
    {
        var context = await InvokeAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
    }

    [Fact]
    public async Task HttpErrorReturns500()
    {
        var context = await InvokeAsync(_ =>
            throw new HttpRequestException("Unavailable", null, HttpStatusCode.ServiceUnavailable));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnhandledExceptionReturns500()
    {
        var context = await InvokeAsync(_ => throw new InvalidOperationException("Simulated failure."));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    private static async Task<HttpContext> InvokeAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext();

        await new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance).InvokeAsync(context);

        return context;
    }
}
