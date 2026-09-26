using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using PaymentGateway.Application.Core.Diagnostics;
using PaymentGateway.Infrastructure;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PaymentGateway.Api.Observability;

internal static class ObservabilityExtensions
{
    public const string ServiceName = "PaymentGateway.Api";
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health/ready";

    private const string DevelopmentTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {CorrelationId} {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.AddLogging();
        builder.AddOpenTelemetry();
        builder.AddCorrelationIdPropagation();
        builder.Services.AddHealthChecks();

        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
            options.GetLevel = (context, _, exception) =>
                exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError ? LogEventLevel.Error
                : IsHealthProbe(context) ? LogEventLevel.Verbose
                : LogEventLevel.Information);

        return app;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        // Liveness runs no checks: it only says the process can serve requests. Restarting the
        // gateway would not fix a bank outage, so dependencies belong in readiness only.
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(DependencyInjection.ReadinessTag)
        });

        return app;
    }

    private static void AddLogging(this WebApplicationBuilder builder) =>
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();

            // Readable text for people locally; one JSON object per line everywhere else, so a
            // log pipeline can index CorrelationId, TraceId and the other properties.
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console(outputTemplate: DevelopmentTemplate);
            }
            else
            {
                configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        },
        // Each host keeps its own logger instead of replacing the process-wide Log.Logger,
        // so several hosts in one process (integration tests) don't log into each other.
        preserveStaticLogger: true);

    private static void AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options => options.Filter = context => !IsHealthProbe(context))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(PaymentMetrics.MeterName)
                // Resilience events: retries, timeouts and circuit breaker state changes.
                .AddMeter("Polly"));

        // Exporting is opt-in through the standard OTEL_* variables, so local runs and tests
        // need no collector.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter();
        }
    }

    private static void AddCorrelationIdPropagation(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdPropagationHandler>();
        builder.Services.ConfigureHttpClientDefaults(client => client.AddHttpMessageHandler<CorrelationIdPropagationHandler>());
    }

    private static bool IsHealthProbe(HttpContext context) => context.Request.Path.StartsWithSegments("/health");
}
