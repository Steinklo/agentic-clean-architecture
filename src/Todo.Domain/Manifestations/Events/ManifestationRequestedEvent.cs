using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>Raised when a Manifestation is recorded against a TodoItem.</summary>
/// <param name="ManifestationId">The Manifestation that was requested.</param>
/// <param name="TodoListId">The TodoList the TodoItem belongs to.</param>
/// <param name="TodoItemId">The TodoItem this Manifestation would make true.</param>
public sealed record ManifestationRequestedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
