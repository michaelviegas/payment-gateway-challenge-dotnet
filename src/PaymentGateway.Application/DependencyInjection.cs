using PaymentGateway.Application.Core.Behaviors;
using PaymentGateway.Application.Core.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PaymentGateway.Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediator(config =>
        {
            config.ServiceLifetime = ServiceLifetime.Scoped;

            // Register pipeline behaviors (order matters)
            config.PipelineBehaviors =
            [
              typeof(RequestLoggingPipelineBehavior<,>),
              typeof(ValidationPipelineBehavior<,>),
              typeof(UnitOfWorkPipelineBehavior<,>)
            ];
        });

        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.AddMetrics();
        builder.Services.AddSingleton<PaymentMetrics>();

        builder.Services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return builder;
    }
}