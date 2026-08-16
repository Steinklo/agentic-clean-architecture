using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation failing.
/// </summary>
/// <remarks>
/// Nothing follows from a failure: the TodoItem is untouched, because the work was not done. See
/// <see cref="ManifestationRequestedEventHandler"/> for why every event has a handler regardless.
/// </remarks>
internal sealed class ManifestationFailedEventHandler(ILogger<ManifestationFailedEventHandler> logger)
    : INotificationHandler<ManifestationFailedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(ManifestationFailedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ManifestationEventLog.ManifestationFailed(
            _logger,
            notification.ManifestationId,
            notification.TodoListId,
            notification.TodoItemId);

        return ValueTask.CompletedTask;
    }
}
