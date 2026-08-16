namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// Base for every endpoint addressing a Manifestation directly.
/// </summary>
/// <remarks>
/// The prefix and the tag are stated once, here, rather than repeated on each endpoint.
/// <para>
/// <b>Not every endpoint in this folder derives from it.</b> Requesting a Manifestation is a named
/// transition on a TodoItem, so it lives under <c>/api/todo-lists/{id}/items/{id}/manifest</c> and
/// derives from <c>TodoListEndpoint</c> instead - because the base class exists to state a route
/// prefix once, and an endpoint under the TodoLists prefix that declared this one would be exactly
/// the drift the rule prevents. Reading and fulfilling address the Manifestation itself, so they
/// sit at its own root and derive from here.
/// </para>
/// </remarks>
internal abstract class ManifestationEndpoint : IEndpoint
{
    /// <summary>
    /// Where a Manifestation lives. A constant because
    /// <see cref="RequestManifestationEndpoint"/> builds its <c>Location</c> header from it while
    /// being mapped into a different group, and two copies of a route would be free to drift.
    /// </summary>
    public const string Prefix = "/api/manifestations";

    /// <inheritdoc />
    public string GroupPrefix => Prefix;

    /// <inheritdoc />
    public string GroupTag => "Manifestations";

    /// <inheritdoc />
    public abstract void MapEndpoint(RouteGroupBuilder group);
}
