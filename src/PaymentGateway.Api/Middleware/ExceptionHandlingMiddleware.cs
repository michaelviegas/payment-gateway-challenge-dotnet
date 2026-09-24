using System.Net;

namespace PaymentGateway.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(
                ex,
                "Bank simulator is unavailable. {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;
        }
    }
}