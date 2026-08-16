using Microsoft.EntityFrameworkCore;
using Todo.Application.Manifestations;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Persistence.Repositories;

/// <summary>
/// Satisfies <see cref="IManifestationRepository"/> against SQL Server through
/// <see cref="TodoDbContext"/>.
/// </summary>
/// <remarks>
/// There is no <c>Include</c> here and there is nothing missing: a Manifestation has no child
/// entities, so the aggregate is the row. It holds the TodoList and TodoItem it refers to as plain
/// ids, and loading those is a different aggregate's repository's job.
/// </remarks>
internal sealed class ManifestationRepository(TodoDbContext context) : IManifestationRepository
{
    private readonly TodoDbContext _context = context;

    public void Add(Manifestation manifestation) => _context.Set<Manifestation>().Add(manifestation);

    public Task<Manifestation?> GetByIdAsync(
        Guid manifestationId,
        CancellationToken cancellationToken = default) =>
        _context
            .Set<Manifestation>()
            .FirstOrDefaultAsync(manifestation => manifestation.Id == manifestationId, cancellationToken);
}
