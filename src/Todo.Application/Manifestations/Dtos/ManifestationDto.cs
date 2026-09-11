using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Dtos;

/// <summary>
/// What a caller of the API sees of a Manifestation.
/// </summary>
/// <remarks>
/// The state is flattened to its name rather than travelling as the enum, for the same reason a
/// value object is flattened to its primitive: <c>System.Text.Json</c> writes an enum as its
/// underlying number by default, which turns a readable contract into one that breaks silently when
/// a member is inserted. Callers — and the integration tests — match on <c>"Requested"</c>.
/// </remarks>
/// <param name="Id">Identity of the Manifestation.</param>
/// <param name="TodoListId">The TodoList owning the TodoItem it refers to.</param>
/// <param name="TodoItemId">The TodoItem it would make true.</param>
/// <param name="State">Requested, Realized or Failed.</param>
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
