namespace Todo.Domain.Common;

/// <summary>
/// Something with identity. Two entities of the same type are equal when their identifiers are equal,
/// regardless of the rest of their state.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Creates the entity with its identity. There is deliberately no parameterless constructor:
    /// EF Core binds the constructor with the fewest property parameters, so an empty one would
    /// silently win over every invariant-bearing constructor in the model.
    /// </summary>
    protected Entity(TId id)
    {
        Id = id;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>The entity's identity.</summary>
    public TId Id { get; private set; }

    /// <summary>When the entity was first created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && other.Id.Equals(Id);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Identity equality.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Identity inequality.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
