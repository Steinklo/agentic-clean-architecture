using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Application.TodoLists.Commands.CompleteTodoItem;
using Todo.Domain.Manifestations.Events;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Reacts to a Manifestation being realized by completing the TodoItem it was about.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the cross-aggregate reaction, and the shape is the point.</b> A realized Manifestation
/// means the work genuinely happened, so the TodoItem should be complete - but a TodoItem belongs to
/// the TodoList aggregate, and one transaction changes one aggregate. So the second aggregate is
/// reached by sending <see cref="CompleteTodoItemCommand"/> from here, not by a handler loading both
/// and saving them together. The event is the join.
/// </para>
/// <para>
/// <b>Where this runs, checked rather than assumed.</b> <c>UnitOfWork</c> dispatches domain events
/// immediately <em>before</em> its save, so this runs inside the fulfilment's unit of work.
/// <see cref="CompleteTodoItemCommand"/>'s own handler ends in a <c>SaveChangesAsync</c> of its own,
/// which - being the same scoped unit of work and the same change tracker - commits the TodoItem
/// change and the Manifestation's together, and the outer save that triggered this then finds
/// nothing left to write. That is one round trip, not two, and no dispatch is lost: events raised by
/// the inner save are picked up by its own dispatch pass before it writes.
/// </para>
/// <para>
/// <b>Nothing here reaches this in production.</b> <c>IRealityGateway</c> always declines, so no
/// Manifestation is ever realized, and this handler is proven only at the aggregate seam - the
/// event's own <c>Realize</c> is asserted in <c>ManifestationTests</c>. Anything reporting that this
/// ran is a bug in the gateway, or in reality.
/// </para>
/// </remarks>
internal sealed class ManifestationRealizedEventHandler(
    ISender sender,
    ILogger<ManifestationRealizedEventHandler> logger)
    : INotificationHandler<ManifestationRealizedEvent>
{
    private readonly ISender _sender = sender;
    private readonly ILogger _logger = logger;

    public async ValueTask Handle(
        ManifestationRealizedEvent notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ManifestationEventLog.ManifestationRealized(
            _logger,
            notification.ManifestationId,
            notification.TodoListId,
            notification.TodoItemId);

        var completion = await _sender
            .Send(
                new CompleteTodoItemCommand(notification.TodoListId, notification.TodoItemId),
                cancellationToken)
            .ConfigureAwait(false);

        // The TodoList gets to refuse - it may be archived, or the TodoItem already complete - and
        // the Manifestation is realized either way, because reality does not roll back. So the
        // disagreement is reported rather than thrown: throwing here would abandon a transaction
        // recording something that has already physically happened.
        if (completion.IsFailure)
        {
            ManifestationEventLog.ManifestationRealizedCompletionRefused(
                _logger,
                notification.ManifestationId,
                notification.TodoItemId,
                completion.Error.Code);
        }
    }
}
