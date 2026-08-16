using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.TodoLists.Events;

namespace Todo.Application.TodoLists.Events;

/// <summary>
/// Reacts to a TodoItem being added to its TodoList.
/// </summary>
/// <remarks>See <see cref="TodoListCreatedEventHandler"/> for why every event has a handler.</remarks>
internal sealed class TodoItemAddedEventHandler(ILogger<TodoItemAddedEventHandler> logger)
    : INotificationHandler<TodoItemAddedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(TodoItemAddedEvent notification, CancellationToken cancellationToken)
    {
        TodoListEventLog.TodoItemAdded(_logger, notification.TodoListId, notification.TodoItemId);

        return ValueTask.CompletedTask;
    }
}
