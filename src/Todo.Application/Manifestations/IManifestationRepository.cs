using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations;

/// <summary>
/// Loads and stores whole Manifestation aggregates.
/// </summary>
/// <remarks>
/// Declared here and implemented in Infrastructure, like every repository in this solution. A
/// Manifestation has no child collections, so there is nothing to <c>Include</c> - loading it whole
/// is loading the row.
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
