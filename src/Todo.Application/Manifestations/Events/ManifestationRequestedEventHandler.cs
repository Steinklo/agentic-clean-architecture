using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation being requested.
/// </summary>
/// <remarks>
/// It logs and nothing else. The handler exists because every domain event needs one - Mediator's
/// generator fails the build with <c>MSG0005</c> otherwise - and because its event id is what the
/// HTTP seam matches on to prove the event was dispatched at all.
/// </remarks>
internal sealed class ManifestationRequestedEventHandler(
    ILogger<ManifestationRequestedEventHandler> logger)
    : INotificationHandler<ManifestationRequestedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(ManifestationRequestedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ManifestationEventLog.ManifestationRequested(
            _logger,
            notification.ManifestationId,
            notification.TodoListId,
            notification.TodoItemId);

        return ValueTask.CompletedTask;
    }
}
