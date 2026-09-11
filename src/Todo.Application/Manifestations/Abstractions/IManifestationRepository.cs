using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Abstractions;

/// <summary>
/// Loads and stores whole Manifestation aggregates.
/// </summary>
/// <remarks>
/// <para>
/// One repository per aggregate root, so this is the second one and the TodoLists repository is not
/// widened to serve both. A Manifestation refers to a TodoItem by id, never by navigation, so
/// nothing here loads a TodoList and there is no <c>Include</c> to write: the aggregate has no
/// children.
/// </para>
/// <para>
/// Nothing here saves. Committing is <see cref="Common.Persistence.IUnitOfWork"/>'s job.
/// </para>
/// </remarks>
public interface IManifestationRepository
{
    /// <summary>
    /// Records a new Manifestation to be inserted by the next save.
    /// </summary>
    /// <param name="manifestation">The Manifestation to add.</param>
    void Add(Manifestation manifestation);

    /// <summary>
    /// Loads a Manifestation by identity, tracked for change so callers can transition it.
    /// </summary>
    /// <param name="manifestationId">The identity to load.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The Manifestation, or <see langword="null"/> when no such Manifestation exists.</returns>
    Task<Manifestation?> GetByIdAsync(Guid manifestationId, CancellationToken cancellationToken = default);
}
