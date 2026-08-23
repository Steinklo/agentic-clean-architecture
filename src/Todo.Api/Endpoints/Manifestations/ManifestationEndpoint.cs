namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// Base for every endpoint addressing a Manifestation directly.
/// </summary>
/// <remarks>
/// Reading and fulfilling address the Manifestation itself, so they sit at its own root. Requesting
/// one does not: it is a named transition on a TodoItem, so
/// <see cref="RequestManifestationEndpoint"/> is a sub-resource of the item and derives from the
/// TodoLists base instead. That is why this feature's folder holds endpoints under two prefixes
/// while each endpoint still takes its prefix from exactly one base class.
/// </remarks>
internal abstract class ManifestationEndpoint : IEndpoint
{
    /// <inheritdoc />
    public string GroupPrefix => "/api/manifestations";

    /// <inheritdoc />
    public string GroupTag => "Manifestations";

    /// <inheritdoc />
    public abstract void MapEndpoint(RouteGroupBuilder group);
}
