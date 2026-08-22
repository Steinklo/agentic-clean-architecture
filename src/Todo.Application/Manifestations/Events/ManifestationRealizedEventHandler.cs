using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Application.TodoLists.Commands.CompleteTodoItem;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation being realized by completing the TodoItem it made true.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the cross-aggregate reaction.</b> A realized Manifestation means the work genuinely
/// happened, so the TodoItem should be complete — but the TodoItem belongs to a TodoList, which is
/// a different aggregate. The rule is one aggregate per transaction, so this handler does not load
/// both and save them together: it sends <see cref="CompleteTodoItemCommand"/>, which is the
/// TodoLists feature's own way in, and lets that use case hold its own aggregate.
/// </para>
/// <para>
/// <b>Ordering, checked against <c>docs/rules/gotchas.md</c> and <c>UnitOfWork</c> rather than assumed.</b>
/// Domain events are dispatched immediately <em>before</em> the save, with each aggregate's events
/// cleared before publishing and the change tracker re-scanned afterwards. So this handler runs
/// while the realized Manifestation is still pending, the command it sends calls
/// <c>SaveChangesAsync</c> itself, and that inner save writes both aggregates — the Manifestation
/// is already tracked in the same scope. The outer save then finds nothing left to write. The loop
/// terminates because events are cleared before they are published, so the
/// <c>TodoItemCompletedEvent</c> raised inside is dispatched on the inner pass and never again.
/// </para>
/// <para>
/// <b>Why a refusal is logged and not thrown.</b> The command can legitimately fail — the TodoItem
/// may already be complete, or its TodoList archived — and none of those is a reason to undo a
/// Manifestation that really did change the world. Throwing would be caught by the
/// unhandled-exception behaviour and turn a recorded fact into a 500. The Manifestation stays
/// realized and the divergence is logged for someone to see.
/// </para>
/// <para>
/// Nothing in this solution can reach this handler: <c>IRealityGateway</c> declines every request,
/// so no Manifestation is ever realized in production. Anything reported as reaching it is a bug in
/// that gateway, or in reality.
/// </para>
/// </remarks>
internal sealed class ManifestationRealizedEventHandler(
    ISender sender,
    ILogger<ManifestationRealizedEventHandler> logger)
    : INotificationHandler<ManifestationRealizedEvent>
{
    private readonly ISender _sender = sender;
    private readonly ILogger _logger = logger;

    public async ValueTask Handle(ManifestationRealizedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ManifestationEventLog.ManifestationRealized(
            _logger,
            notification.ManifestationId,
            notification.TodoItemId,
            notification.TodoListId);

        var completion = await _sender
            .Send(
                new CompleteTodoItemCommand(notification.TodoListId, notification.TodoItemId),
                cancellationToken)
            .ConfigureAwait(false);

        if (completion.IsFailure)
        {
            ManifestationEventLog.ManifestationCompletionRefused(
                _logger,
                notification.ManifestationId,
                notification.TodoItemId,
                completion.Error.Code);
        }
    }
}
