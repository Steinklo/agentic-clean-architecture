using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.Events;

/// <summary>Raised when a TodoItem is completed through its TodoList.</summary>
/// <param name="TodoListId">The owning TodoList.</param>
/// <param name="TodoItemId">The TodoItem that was completed.</param>
public sealed record TodoItemCompletedEvent(Guid TodoListId, Guid TodoItemId) : DomainEvent(TodoListId);
