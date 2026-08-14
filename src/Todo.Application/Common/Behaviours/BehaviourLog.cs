using Microsoft.Extensions.Logging;

namespace Todo.Application.Common.Behaviours;

/// <summary>
/// Every log message the pipeline behaviours emit, declared once.
/// </summary>
/// <remarks>
/// Source-generated with <see cref="LoggerMessageAttribute"/> rather than written as
/// <c>logger.LogInformation("...", args)</c>: the generated methods allocate nothing when the
/// level is disabled, and the message templates are checked at compile time. The behaviours are
/// generic, and the generator does not emit into generic types, so the messages live here in a
/// non-generic class instead of on each behaviour.
/// </remarks>
internal static partial class BehaviourLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Handling {RequestName}")]
    public static partial void Handling(ILogger logger, string requestName);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled exception while handling {RequestName}")]
    public static partial void UnhandledException(ILogger logger, string requestName, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Long running request: {RequestName} took {ElapsedMilliseconds} ms")]
    public static partial void LongRunningRequest(ILogger logger, string requestName, long elapsedMilliseconds);
}
