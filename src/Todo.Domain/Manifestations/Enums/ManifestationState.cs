namespace Todo.Domain.Manifestations.Enums;

/// <summary>
/// Where a Manifestation has got to. <see cref="Realized"/> and <see cref="Failed"/> are terminal:
/// a Manifestation that has reached either never leaves it.
/// </summary>
public enum ManifestationState
{
    /// <summary>Recorded, and not yet attempted against reality.</summary>
    Requested,

    /// <summary>Reality was changed. Terminal.</summary>
    Realized,

    /// <summary>Reality declined, or could not be reached. Terminal.</summary>
    Failed
}
