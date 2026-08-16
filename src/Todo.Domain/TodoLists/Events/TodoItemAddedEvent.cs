using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.Events;

/// <summary>Raised when a TodoItem is added to its TodoList.</summary>
/// <param name="TodoListId">The owning TodoList.</param>
/// <param name="TodoItemId">The TodoItem that was added.</param>
/// <param name="Description">The description it was added with.</param>
public sealed record TodoItemAddedEvent(Guid TodoListId, Guid TodoItemId, string Description)
    : DomainEvent(TodoListId);
