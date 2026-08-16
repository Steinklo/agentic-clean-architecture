using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.TodoLists.Events;

namespace Todo.Application.TodoLists.Events;

/// <summary>
/// Reacts to a TodoItem being completed through its TodoList.
/// </summary>
/// <remarks>See <see cref="TodoListCreatedEventHandler"/> for why every event has a handler.</remarks>
internal sealed class TodoItemCompletedEventHandler(ILogger<TodoItemCompletedEventHandler> logger)
    : INotificationHandler<TodoItemCompletedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
    {
        TodoListEventLog.TodoItemCompleted(_logger, notification.TodoListId, notification.TodoItemId);

        return ValueTask.CompletedTask;
    }
}
