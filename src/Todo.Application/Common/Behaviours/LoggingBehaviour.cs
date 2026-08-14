using Mediator;
using Microsoft.Extensions.Logging;

namespace Todo.Application.Common.Behaviours;

/// <summary>
/// Records that a request entered the pipeline.
/// </summary>
/// <remarks>
/// Deliberately not <c>async</c>: it has nothing to do after the handler returns, so it hands the
/// <see cref="ValueTask{TResult}"/> straight back rather than adding a state machine to every
/// request in the application.
/// </remarks>
/// <typeparam name="TRequest">The request being handled.</typeparam>
/// <typeparam name="TResponse">What the handler returns.</typeparam>
internal sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    private readonly ILogger _logger = logger;

    public ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        BehaviourLog.Handling(_logger, typeof(TRequest).Name);

        return next(message, cancellationToken);
    }
}
