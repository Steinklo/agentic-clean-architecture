using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Todo.Application.Common.Behaviours;

/// <summary>
/// Times every request and warns about the slow ones.
/// </summary>
/// <remarks>
/// Innermost of the four behaviours, so the number it reports is the handler's own cost rather
/// than the cost of validating a request that was then rejected.
/// </remarks>
/// <typeparam name="TRequest">The request being handled.</typeparam>
/// <typeparam name="TResponse">What the handler returns.</typeparam>
internal sealed class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    /// <summary>Above this, a request is worth a look. Below it, timing is noise.</summary>
    private const int LongRunningThresholdMilliseconds = 500;

    private readonly ILogger _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();

        var response = await next(message, cancellationToken).ConfigureAwait(false);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (elapsed.TotalMilliseconds > LongRunningThresholdMilliseconds)
        {
            BehaviourLog.LongRunningRequest(_logger, typeof(TRequest).Name, (long)elapsed.TotalMilliseconds);
        }

        return response;
    }
}
