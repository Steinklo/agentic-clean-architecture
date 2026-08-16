using Mediator;

namespace Todo.Domain.Common;

/// <summary>
/// Something that happened in the domain, recorded by an aggregate root and dispatched by the
/// unit of work immediately before the save, so handler changes join the same transaction.
/// </summary>
/// <param name="AggregateId">The identity of the aggregate the event happened to.</param>
public abstract record DomainEvent(Guid AggregateId) : INotification
{
    /// <summary>Identity of this occurrence.</summary>
    public Guid Id { get; } = Guid.CreateVersion7();

    /// <summary>When the event happened.</summary>
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
