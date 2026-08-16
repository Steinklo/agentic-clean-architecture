using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>
/// Raised when a Manifestation records that the attempt to make its TodoItem true did not succeed.
/// </summary>
/// <param name="ManifestationId">The Manifestation that failed.</param>
/// <param name="TodoListId">The TodoList owning the TodoItem it is about.</param>
/// <param name="TodoItemId">The TodoItem that was not made true.</param>
public sealed record ManifestationFailedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
