namespace Todo.Domain.Common;

/// <summary>
/// Something with no identity, equal to another instance when all of its components are equal.
/// </summary>
/// <remarks>
/// Deliberately an abstract class rather than a record or struct: EF Core maps these as complex
/// types, and their constructors must take scalars only — a nested complex type cannot bind to a
/// constructor parameter (efcore#31621). Value object properties must also be required rather than
/// optional, because EF 10's nullable complex-type support still has open defects.
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The components that, taken together, define this value.</summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject valueObject && Equals(valueObject);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var component in GetEqualityComponents())
        {
            hashCode.Add(component);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>Component-wise equality.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Component-wise inequality.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
