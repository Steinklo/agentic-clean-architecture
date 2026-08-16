namespace Todo.Domain.Manifestations;

/// <summary>
/// Where a <see cref="Manifestation"/> has got to. Two of the three are terminal, which is the
/// invariant the aggregate exists to enforce.
/// </summary>
public enum ManifestationState
{
    /// <summary>Someone has asked for the TodoItem to be made true, and nothing has happened yet.</summary>
    Requested,

    /// <summary>The TodoItem was made true in the physical world. Terminal.</summary>
    Realized,

    /// <summary>The attempt to make the TodoItem true did not succeed. Terminal.</summary>
    Failed
}
