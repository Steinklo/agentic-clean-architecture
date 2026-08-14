using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.Events;

/// <summary>Raised when a TodoList is archived.</summary>
/// <param name="TodoListId">The TodoList that was archived.</param>
public sealed record TodoListArchivedEvent(Guid TodoListId) : DomainEvent(TodoListId);
