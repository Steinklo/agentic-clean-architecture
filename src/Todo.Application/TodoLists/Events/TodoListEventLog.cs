using Microsoft.Extensions.Logging;

namespace Todo.Application.TodoLists.Events;

/// <summary>
/// Log messages emitted by the TodoList domain-event handlers.
/// </summary>
internal static partial class TodoListEventLog
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "TodoList {TodoListId} was created with title '{Title}'")]
    public static partial void TodoListCreated(ILogger logger, Guid todoListId, string title);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "TodoItem {TodoItemId} was added to TodoList {TodoListId}")]
    public static partial void TodoItemAdded(ILogger logger, Guid todoListId, Guid todoItemId);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "TodoItem {TodoItemId} on TodoList {TodoListId} was completed")]
    public static partial void TodoItemCompleted(ILogger logger, Guid todoListId, Guid todoItemId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "TodoList {TodoListId} was archived")]
    public static partial void TodoListArchived(ILogger logger, Guid todoListId);
}
