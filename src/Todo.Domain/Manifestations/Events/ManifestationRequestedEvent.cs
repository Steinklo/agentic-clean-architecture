using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>Raised when a Manifestation of a TodoItem is requested.</summary>
/// <param name="ManifestationId">The Manifestation that was requested.</param>
/// <param name="TodoListId">The TodoList owning the TodoItem it is about.</param>
/// <param name="TodoItemId">The TodoItem it asks to be made true.</param>
public sealed record ManifestationRequestedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
