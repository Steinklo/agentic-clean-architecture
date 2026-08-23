using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation being requested against a TodoItem.
/// </summary>
/// <remarks>
/// It only logs, and that is the whole job: the record that a Manifestation was asked for is the
/// audit trail, and its event id is the seam the HTTP tests assert dispatch on. Mediator has no
/// concept of an optional handler, so this arrives with the event either way.
/// </remarks>
internal sealed class ManifestationRequestedEventHandler(ILogger<ManifestationRequestedEventHandler> logger)
    : INotificationHandler<ManifestationRequestedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(ManifestationRequestedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ManifestationEventLog.ManifestationRequested(
            _logger,
            notification.ManifestationId,
            notification.TodoItemId,
            notification.TodoListId);

        return ValueTask.CompletedTask;
    }
}
