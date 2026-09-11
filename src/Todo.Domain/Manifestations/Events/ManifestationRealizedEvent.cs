using Todo.Domain.Common;

namespace Todo.Domain.Manifestations.Events;

/// <summary>
/// Raised when a Manifestation is realized — the TodoItem is now true in the physical world.
/// </summary>
/// <remarks>
/// This is the event that crosses an aggregate boundary: the work genuinely happened, so the
/// TodoItem should be complete, and the TodoList is a second aggregate reached in a second
/// transaction by a handler rather than loaded and saved alongside this one.
/// </remarks>
/// <param name="ManifestationId">The Manifestation that was realized.</param>
/// <param name="TodoListId">The TodoList the TodoItem belongs to.</param>
/// <param name="TodoItemId">The TodoItem that is now true.</param>
public sealed record ManifestationRealizedEvent(Guid ManifestationId, Guid TodoListId, Guid TodoItemId)
    : DomainEvent(ManifestationId);
