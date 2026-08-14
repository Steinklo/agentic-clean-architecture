using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Todo.Application.Common.Behaviours;
using Todo.Application.TodoLists.Commands.CreateTodoList;

namespace Todo.Application;

/// <summary>
/// Composition root for everything Application owns.
/// </summary>
/// <remarks>
/// Named <c>ConfigureServices</c> in every layer, at the project root, so the composition entry
/// point is in the same place whichever layer you open.
/// </remarks>
public static class ConfigureServices
{
    /// <summary>
    /// Registers the request pipeline, its behaviours, and the shape validators.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The scan is what makes validation real. Adding a validator next to its command is
        // enough - there is no list to remember to update, so a validator cannot be written and
        // then silently never run.
        services.AddValidatorsFromAssemblyContaining<CreateTodoListCommandValidator>();

        services.AddMediator(options =>
        {
            // Scoped, so a handler and the unit of work it commits share one DbContext, and so
            // handlers may depend on scoped services at all. Mediator's default is Singleton,
            // which cannot resolve either.
            options.ServiceLifetime = ServiceLifetime.Scoped;

            // Mediator is source-generated: it discovers requests, handlers and notifications by
            // compiling them, so there is no assembly scan and no marker type here. Behaviours are
            // the exception - they are NOT discovered, and a behaviour that is written but left
            // out of this list simply never runs.
            //
            // Order is outermost first, and it is chosen:
            //   UnhandledException  sees every failure, including ones the others cause.
            //   Logging             records the request before anything can reject it.
            //   Validation          rejects malformed requests before a handler is entered.
            //   Performance         times the handler alone, not the requests we refused.
            options.PipelineBehaviors =
            [
                typeof(UnhandledExceptionBehaviour<,>),
                typeof(LoggingBehaviour<,>),
                typeof(ValidationBehaviour<,>),
                typeof(PerformanceBehaviour<,>),
            ];
        });

        return services;
    }
}
