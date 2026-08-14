using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Todo.IntegrationTests;

/// <summary>
/// One thing the in-process host logged, kept in the order it was logged.
/// </summary>
/// <param name="Category">The logger category, which names the component that emitted it.</param>
/// <param name="EventId">The event id, which is what a structured assertion should match on.</param>
/// <param name="Message">The formatted message.</param>
public sealed record LogRecord(string Category, EventId EventId, string Message);

/// <summary>
/// Records what the host logs, in order, so a test can assert on the <em>sequence</em> of things
/// that happened inside a single request.
/// </summary>
/// <remarks>
/// <para>
/// This exists for one assertion that cannot be made any other way from the HTTP seam: that a domain
/// event is dispatched inside the same transaction as the save that commits it. The unit of work
/// publishes events and then calls <c>SaveChangesAsync</c> once, so "in the same transaction" is
/// observable as an ordering - the event handler runs, and only afterwards does the database see a
/// single write. Both halves of that are logged by code the test does not own, so recording the log
/// is what turns an implementation detail into an observation.
/// </para>
/// <para>
/// Recording is off unless a test opens it, so every other test pays nothing. That also keeps the
/// records scoped: <see cref="Record"/> clears first, so what is captured is exactly what happened
/// between the two braces. Safe on the shared fixture because xUnit runs a collection's classes one
/// at a time.
/// </para>
/// </remarks>
public sealed class LogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<LogRecord> _records = new();

    private volatile bool _recording;

    /// <summary>
    /// What was logged during the open recording, oldest first.
    /// </summary>
    public IReadOnlyList<LogRecord> Records => [.. _records];

    /// <summary>
    /// Starts recording, discarding anything captured before now.
    /// </summary>
    /// <returns>A handle that stops recording when disposed.</returns>
    public IDisposable Record()
    {
        _records.Clear();
        _recording = true;

        return new Recording(this);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);

    private sealed class Recording(LogCapture capture) : IDisposable
    {
        private readonly LogCapture _capture = capture;

        public void Dispose()
        {
            _capture._recording = false;
            GC.SuppressFinalize(this);
        }
    }

    private sealed class CapturingLogger(LogCapture capture, string category) : ILogger
    {
        private readonly LogCapture _capture = capture;
        private readonly string _category = category;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => _capture._recording;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel))
            {
                return;
            }

            _capture._records.Enqueue(new LogRecord(_category, eventId, formatter(state, exception)));
        }
    }
}
