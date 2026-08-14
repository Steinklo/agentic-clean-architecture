using Mediator;
using Microsoft.Extensions.Logging;
using Todo.Domain.TodoLists.Events;

namespace Todo.Application.TodoLists.Events;

/// <summary>
/// Reacts to a TodoList being archived.
/// </summary>
/// <remarks>See <see cref="TodoListCreatedEventHandler"/> for why every event has a handler.</remarks>
internal sealed class TodoListArchivedEventHandler(ILogger<TodoListArchivedEventHandler> logger)
    : INotificationHandler<TodoListArchivedEvent>
{
    private readonly ILogger _logger = logger;

    public ValueTask Handle(TodoListArchivedEvent notification, CancellationToken cancellationToken)
    {
        TodoListEventLog.TodoListArchived(_logger, notification.TodoListId);

        return ValueTask.CompletedTask;
    }
}
