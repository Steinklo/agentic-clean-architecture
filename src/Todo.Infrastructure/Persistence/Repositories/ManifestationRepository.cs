using Microsoft.EntityFrameworkCore;
using Todo.Application.Manifestations.Abstractions;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Persistence.Repositories;

/// <summary>
/// Satisfies <see cref="IManifestationRepository"/> against SQL Server through
/// <see cref="TodoDbContext"/>.
/// </summary>
/// <remarks>
/// No <c>Include</c>, because a Manifestation has no children — it refers to its TodoItem by id and
/// never navigates to it. Loading it whole is loading its own row.
/// <para>
/// Tracked, like every repository here: the aggregate is loaded so it can be transitioned and
/// saved. <c>Set&lt;T&gt;()</c> rather than a <c>DbSet</c> property, so adding this aggregate
/// changed nothing that already existed.
/// </para>
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
