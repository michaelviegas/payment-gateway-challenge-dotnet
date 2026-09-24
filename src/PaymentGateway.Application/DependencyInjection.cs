using PaymentGateway.Application.Core.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
              typeof(ExceptionHandlingPipelineBehavior<,>),
              typeof(RequestLoggingPipelineBehavior<,>),
              typeof(ValidationPipelineBehavior<,>),
              typeof(IdempotencyPipelineBehavior<,>),
              typeof(UnitOfWorkPipelineBehavior<,>)
            ];
        });

        builder.Services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return builder;
    }
}