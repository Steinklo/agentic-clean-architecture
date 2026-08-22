using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>Raised when a Manifestation is abandoned — reality declined, or could not be reached.</summary>
/// <param name="ManifestationId">The Manifestation that failed.</param>
/// <param name="TodoListId">The TodoList the TodoItem belongs to.</param>
/// <param name="TodoItemId">The TodoItem that stays untrue.</param>
public sealed record ManifestationFailedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
