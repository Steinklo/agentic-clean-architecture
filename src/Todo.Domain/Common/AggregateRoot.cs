namespace Todo.Domain.Common;

/// <summary>
/// The entry point to an aggregate: the only object outside code may hold a reference to,
/// and the one responsible for every invariant spanning the objects inside it.
/// The domain-event collection lives here, once, rather than being repeated in every aggregate.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>Creates the aggregate root with its identity.</summary>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// Events raised since the aggregate was loaded. Must be <c>Ignore</c>d in EF Core entity
    /// configuration, or the relationship convention maps it as a navigation with its own table.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records an event to be dispatched when the aggregate is saved.</summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Drops the recorded events, called by the unit of work once they are dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
