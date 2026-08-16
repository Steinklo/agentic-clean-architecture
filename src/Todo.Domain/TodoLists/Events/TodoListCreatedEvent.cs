using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.Events;

/// <summary>Raised when a TodoList is created.</summary>
/// <param name="TodoListId">The new TodoList.</param>
/// <param name="Title">Its title at the moment of creation.</param>
public sealed record TodoListCreatedEvent(Guid TodoListId, string Title) : DomainEvent(TodoListId);
