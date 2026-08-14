using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.TodoLists.Events;

namespace Todo.Application.TodoLists.Events;

/// <summary>
/// Reacts to a TodoList being created.
/// </summary>
/// <remarks>
/// <para>
/// Domain-event handlers run <em>inside</em> the unit of work, immediately before the save that
/// persists the change which raised the event. Anything a handler does to a tracked aggregate is
/// therefore written by that same save, in the same transaction - which is what makes reacting to
/// an event safe rather than a second, independently-failing write.
/// </para>
/// <para>
/// Every notification type must have a handler: Mediator's source generator reports
/// <c>MSG0005</c> for one that has none, and warnings are errors here. So a handler exists for
/// each domain event from the moment the event does, even when - as here - all it has to say is
/// that the event happened.
/// </para>
/// </remarks>
internal sealed class TodoListCreatedEventHandler(ILogger<TodoListCreatedEventHandler> logger)
    : INotificationHandler<TodoListCreatedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(TodoListCreatedEvent notification, CancellationToken cancellationToken)
    {
        TodoListEventLog.TodoListCreated(_logger, notification.TodoListId, notification.Title);

        return ValueTask.CompletedTask;
    }
}
