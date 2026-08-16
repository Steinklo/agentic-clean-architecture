using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>
/// Raised when a Manifestation records that its TodoItem was made true in the physical world.
/// </summary>
/// <remarks>
/// Carries the TodoList and TodoItem ids because the handler for it has to reach the other
/// aggregate, and an event carrying primitives is the only reference across that boundary.
/// </remarks>
/// <param name="ManifestationId">The Manifestation that was realized.</param>
/// <param name="TodoListId">The TodoList owning the TodoItem it is about.</param>
/// <param name="TodoItemId">The TodoItem that was made true.</param>
public sealed record ManifestationRealizedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
