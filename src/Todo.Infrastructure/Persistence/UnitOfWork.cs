using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Domain.Common;

namespace Todo.Infrastructure.Persistence;

/// <summary>
/// Commits the current scope's changes, dispatching domain events immediately before the write.
/// </summary>
/// <remarks>
/// <para>
/// This is the dotnet/eShop ordering: dispatch, <em>then</em> save. The handlers run while the
/// change tracker still holds every pending change, so anything a handler does to a tracked
/// aggregate is written by the single <c>SaveChangesAsync</c> below - one round trip, one implicit
/// transaction, all or nothing. Dispatching after the save would instead give you a second write
/// that can fail on its own and leave the two halves of an event disagreeing.
/// </para>
/// <para>
/// Events are cleared from the aggregate <em>before</em> being published, not after. That is what
/// makes the loop terminate: a handler is free to raise further events on the aggregates it
/// touches, those are picked up by the next pass, and an event already published can never be
/// published twice.
/// </para>
/// </remarks>
internal sealed class UnitOfWork(TodoDbContext context, IPublisher publisher) : IUnitOfWork
{
    private readonly TodoDbContext _context = context;
    private readonly IPublisher _publisher = publisher;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);

        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Every aggregate in this solution is identified by a Guid - the domain mints identities
        // with Guid.CreateVersion7() - so this closed generic finds all of them.
        while (true)
        {
            var aggregates = _context
                .ChangeTracker
                .Entries<AggregateRoot<Guid>>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
            {
                return;
            }

            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                // Published as object on purpose: that overload resolves the handler from the
                // runtime type, so a DomainEvent-typed reference still reaches the handler
                // registered for TodoListCreatedEvent rather than looking for one keyed on the
                // abstract base.
                await _publisher.Publish((object)domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
