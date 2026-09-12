using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation being abandoned.
/// </summary>
/// <remarks>
/// Deliberately does nothing to the TodoList. A Manifestation failing says nothing about the
/// TodoItem — it stays incomplete, which it already was — so there is no reaction to have. The
/// asymmetry with <see cref="ManifestationRealizedEventHandler"/> is the point: only the realized
/// path carries news the other aggregate needs.
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
            notification.TodoItemId,
            notification.TodoListId);

        return ValueTask.CompletedTask;
    }
}
