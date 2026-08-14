using Mediator;
using Microsoft.Extensions.Logging;

namespace Todo.Application.Common.Behaviours;

/// <summary>
/// The outermost behaviour: records any exception that escaped a handler, then lets it escape.
/// </summary>
/// <remarks>
/// Only <em>unexpected</em> failures reach here. Expected ones - an invalid title, an unknown
/// identifier, an aggregate refusing an operation - are returned as a failed
/// <see cref="Todo.Domain.Common.Result"/> and never travel as exceptions.
/// </remarks>
/// <typeparam name="TRequest">The request being handled.</typeparam>
/// <typeparam name="TResponse">What the handler returns.</typeparam>
internal sealed class UnhandledExceptionBehaviour<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    private readonly ILogger _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            BehaviourLog.UnhandledException(_logger, typeof(TRequest).Name, exception);
            throw;
        }
    }
}
