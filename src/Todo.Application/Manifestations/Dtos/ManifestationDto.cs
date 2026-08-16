using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Dtos;

/// <summary>
/// What a caller of the API sees of a Manifestation.
/// </summary>
/// <remarks>
/// The TodoList and TodoItem travel as identifiers, not as a nested TodoList, because that is
/// exactly how the aggregate holds them: a Manifestation refers to a TodoItem across an aggregate
/// boundary, and a projection that inlined the other aggregate would suggest one transaction covers
/// both.
/// <para>
/// <see cref="ManifestationState"/> is projected as its name rather than its ordinal, so reordering
/// the enum cannot silently change what a caller reads.
/// </para>
/// </remarks>
/// <param name="Id">Identity of the Manifestation.</param>
/// <param name="TodoListId">The TodoList owning the TodoItem it is about.</param>
/// <param name="TodoItemId">The TodoItem it asks to be made true.</param>
/// <param name="State">How far it has got: Requested, Realized or Failed.</param>
/// <param name="CreatedAt">When it was requested.</param>
public sealed record ManifestationDto(
    Guid Id,
    Guid TodoListId,
    Guid TodoItemId,
    string State,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Projects a Manifestation aggregate onto its API representation.
    /// </summary>
    /// <param name="manifestation">The aggregate to project.</param>
    /// <returns>The DTO.</returns>
    public static ManifestationDto FromDomain(Manifestation manifestation)
    {
        ArgumentNullException.ThrowIfNull(manifestation);

        return new ManifestationDto(
            manifestation.Id,
            manifestation.TodoListId,
            manifestation.TodoItemId,
            manifestation.State.ToString(),
            manifestation.CreatedAt);
    }
}
